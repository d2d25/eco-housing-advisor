using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class EcoHousingRuleAudit
    {
        private const double Tolerance = 0.001;

        public string Run()
        {
            var lines = new List<string> { "Eco Housing Advisor rule audit:" };
            var expected = HousingRoomRules.ExpectedRuleSpecs()
                .ToDictionary(rule => rule.Category, StringComparer.OrdinalIgnoreCase);

            var runtimeRules = ReadRuntimeRules(expected.Keys);
            AddComparison(lines, "runtime HousingConfig", expected, runtimeRules);

            var sourcePath = FindHousingValuesPath();
            if (sourcePath == null)
            {
                lines.Add("WARN source HousingValues.cs not found; source audit skipped.");
            }
            else
            {
                lines.Add("Source: " + sourcePath);
                AddComparison(lines, "source HousingValues.cs", expected, ReadSourceRules(sourcePath));
            }

            lines.Add(lines.Any(line => line.StartsWith("FAIL ", StringComparison.Ordinal))
                ? "Result: FAIL - Eco rules differ from Eco Housing Advisor domain rules."
                : "Result: PASS - audited Eco housing rules match the advisor domain rules.");
            return string.Join(Environment.NewLine, lines);
        }

        private static void AddComparison(
            ICollection<string> lines,
            string label,
            IReadOnlyDictionary<string, HousingRoomRuleSpec> expected,
            IReadOnlyDictionary<string, HousingRoomRuleSpec> actual)
        {
            if (actual.Count == 0)
            {
                lines.Add("WARN " + label + " audit unavailable.");
                return;
            }

            foreach (var entry in expected.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
            {
                if (!actual.TryGetValue(entry.Key, out var got))
                {
                    lines.Add("FAIL " + label + ": missing category " + entry.Key + ".");
                    continue;
                }

                Compare(lines, label, entry.Value, got);
            }

            foreach (var extra in actual.Keys.Where(key => !expected.ContainsKey(key)).OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add("FAIL " + label + ": unexpected category " + extra + ".");
            }
        }

        private static void Compare(ICollection<string> lines, string label, HousingRoomRuleSpec expected, HousingRoomRuleSpec actual)
        {
            CompareBool(lines, label, expected.Category, "CanBeRoomCategory", expected.CanBeRoomCategory, actual.CanBeRoomCategory);
            CompareBool(lines, label, expected.Category, "SupportForAnyRoomType", expected.SupportForAnyRoom, actual.SupportForAnyRoom);
            CompareBool(lines, label, expected.Category, "NegatesValue", expected.NegatesValue, actual.NegatesValue);
            CompareDouble(lines, label, expected.Category, "MaxSupportPercentOfPrimary", expected.MaxSupportPercentOfPrimary, actual.MaxSupportPercentOfPrimary);
            CompareDouble(lines, label, expected.Category, "CapToPercentOfRestOfProperty", expected.CapToPercentOfRestOfProperty, actual.CapToPercentOfRestOfProperty);

            var expectedSupport = string.Join(",", expected.SupportingRoomCategoryNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            var actualSupport = string.Join(",", actual.SupportingRoomCategoryNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase));
            if (!string.Equals(expectedSupport, actualSupport, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "FAIL {0}: {1}.SupportingRoomCategoryNames expected [{2}], got [{3}].",
                    label,
                    expected.Category,
                    expectedSupport,
                    actualSupport));
            }
        }

        private static void CompareBool(ICollection<string> lines, string label, string category, string member, bool expected, bool actual)
        {
            if (expected != actual)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture, "FAIL {0}: {1}.{2} expected {3}, got {4}.", label, category, member, expected, actual));
            }
        }

        private static void CompareDouble(ICollection<string> lines, string label, string category, string member, double expected, double actual)
        {
            if (Math.Abs(expected - actual) > Tolerance)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture, "FAIL {0}: {1}.{2} expected {3}, got {4}.", label, category, member, expected, actual));
            }
        }

        private static IReadOnlyDictionary<string, HousingRoomRuleSpec> ReadRuntimeRules(IEnumerable<string> categoryNames)
        {
            var configType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type => string.Equals(type.FullName, "Eco.Gameplay.Housing.PropertyValues.HousingConfig", StringComparison.Ordinal));
            var method = configType?.GetMethod("GetRoomCategory", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (method == null)
            {
                return new Dictionary<string, HousingRoomRuleSpec>(StringComparer.OrdinalIgnoreCase);
            }

            var rules = new Dictionary<string, HousingRoomRuleSpec>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in categoryNames)
            {
                object roomCategory = null;
                try { roomCategory = method.Invoke(null, new object[] { category }); }
                catch { }

                if (roomCategory == null)
                {
                    continue;
                }

                rules[category] = new HousingRoomRuleSpec(
                    category,
                    ReadBool(roomCategory, "CanBeRoomCategory", true),
                    ReadBool(roomCategory, "SupportForAnyRoomType", false),
                    ReadBool(roomCategory, "NegatesValue", false),
                    ReadDouble(roomCategory, "MaxSupportPercentOfPrimary", 1),
                    ReadDouble(roomCategory, "CapToPercentOfRestOfProperty", 0),
                    ReadStringEnumerable(roomCategory, "SupportingRoomCategoryNames"));
            }

            return rules;
        }

        private static IReadOnlyDictionary<string, HousingRoomRuleSpec> ReadSourceRules(string path)
        {
            var rules = new Dictionary<string, HousingRoomRuleSpec>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path).Where(line => line.IndexOf("new RoomCategory()", StringComparison.Ordinal) >= 0))
            {
                var category = ReadQuotedValueAfter(line, "DisplayName = Localizer.DoStr(");
                if (string.IsNullOrWhiteSpace(category))
                {
                    continue;
                }

                rules[category] = new HousingRoomRuleSpec(
                    category,
                    !line.Contains("CanBeRoomCategory = false", StringComparison.Ordinal),
                    line.Contains("SupportForAnyRoomType = true", StringComparison.Ordinal),
                    line.Contains("NegatesValue = true", StringComparison.Ordinal),
                    ReadAssignedDouble(line, "MaxSupportPercentOfPrimary", 1),
                    ReadAssignedDouble(line, "CapToPercentOfRestOfProperty", 0),
                    ReadStringArrayAssignment(line, "SupportingRoomCategoryNames"));
            }

            return rules;
        }

        private static string FindHousingValuesPath()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.CurrentDirectory, "Mods", "__core__", "Systems", "HousingValues.cs"),
                @"C:\Program Files (x86)\Steam\steamapps\common\Eco Server\Mods\__core__\Systems\HousingValues.cs",
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).Cast<Type>();
            }
        }

        private static bool ReadBool(object instance, string memberName, bool fallback)
        {
            var value = ReadMember(instance, memberName);
            return value is bool b ? b : fallback;
        }

        private static double ReadDouble(object instance, string memberName, double fallback)
        {
            var value = ReadMember(instance, memberName);
            if (value == null)
            {
                return fallback;
            }

            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        private static IReadOnlyList<string> ReadStringEnumerable(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            if (value is IEnumerable<string> strings)
            {
                return strings.ToArray();
            }

            return new string[0];
        }

        private static object ReadMember(object instance, string memberName)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = instance.GetType();
            var property = type.GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(instance);
            var field = type.GetField(memberName, Flags);
            return field == null ? null : field.GetValue(instance);
        }

        private static string ReadQuotedValueAfter(string text, string marker)
        {
            var start = text.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            start = text.IndexOf('"', start);
            if (start < 0)
            {
                return null;
            }

            var end = text.IndexOf('"', start + 1);
            return end < 0 ? null : text.Substring(start + 1, end - start - 1);
        }

        private static double ReadAssignedDouble(string text, string memberName, double fallback)
        {
            var start = text.IndexOf(memberName, StringComparison.Ordinal);
            if (start < 0)
            {
                return fallback;
            }

            start = text.IndexOf('=', start);
            if (start < 0)
            {
                return fallback;
            }

            start++;
            while (start < text.Length && char.IsWhiteSpace(text[start]))
            {
                start++;
            }

            var end = start;
            while (end < text.Length && (char.IsDigit(text[end]) || text[end] == '.' || text[end] == '-'))
            {
                end++;
            }

            var value = text.Substring(start, end - start);
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            value = value.StartsWith(".", StringComparison.Ordinal) ? "0" + value : value;
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static IReadOnlyList<string> ReadStringArrayAssignment(string text, string memberName)
        {
            var start = text.IndexOf(memberName, StringComparison.Ordinal);
            if (start < 0)
            {
                return new string[0];
            }

            start = text.IndexOf('{', start);
            var end = start < 0 ? -1 : text.IndexOf('}', start + 1);
            if (start < 0 || end < 0)
            {
                return new string[0];
            }

            var raw = text.Substring(start + 1, end - start - 1);
            var values = new List<string>();
            var cursor = 0;
            while (cursor < raw.Length)
            {
                var quoteStart = raw.IndexOf('"', cursor);
                if (quoteStart < 0)
                {
                    break;
                }

                var quoteEnd = raw.IndexOf('"', quoteStart + 1);
                if (quoteEnd < 0)
                {
                    break;
                }

                values.Add(raw.Substring(quoteStart + 1, quoteEnd - quoteStart - 1));
                cursor = quoteEnd + 1;
            }

            return values.ToArray();
        }
    }
}

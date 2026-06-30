using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class EcoPropertyValueReader
    {
        public HousingPropertyValueSnapshot Read(object propertyValue)
        {
            var warnings = new List<string>();
            var rooms = new List<HousingPropertyRoomValue>();
            if (propertyValue == null)
            {
                return new HousingPropertyValueSnapshot("null", null, null, null, rooms, new[] { "PropertyValue was null." }, DateTimeOffset.UtcNow);
            }

            var totalValue = ReadTotalValue(propertyValue);
            var residentCount = CountEnumerable(ReadMember(ReadMember(ReadMember(propertyValue, "Deed"), "Residency"), "Residents"));
            var explicitRooms = ReadMember(propertyValue, "Rooms");
            foreach (var room in ReadRoomValues(explicitRooms))
            {
                if (!rooms.Any(existing => SameRoom(existing, room)))
                {
                    rooms.Add(room);
                }
            }

            if (rooms.Count == 0)
            {
                foreach (var room in ReadRoomValues(ReadMember(propertyValue, "RoomValues")))
                {
                    if (!rooms.Any(existing => SameRoom(existing, room)))
                    {
                        rooms.Add(room);
                    }
                }
            }

            var roomSum = rooms.Sum(room => room.Value ?? 0);
            var finalMultiplier = totalValue != null && roomSum > 0
                ? totalValue.Value / roomSum
                : (double?)null;

            if (rooms.Count == 0)
            {
                warnings.Add("PropertyValue room list was not readable yet; runtime member mapping needs confirmation on this Eco version.");
            }

            return new HousingPropertyValueSnapshot(
                propertyValue.GetType().FullName ?? propertyValue.GetType().Name,
                totalValue,
                finalMultiplier,
                residentCount,
                rooms,
                warnings,
                DateTimeOffset.UtcNow);
        }

        private static bool SameRoom(HousingPropertyRoomValue left, HousingPropertyRoomValue right)
        {
            return string.Equals(left.RoomName, right.RoomName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Category, right.Category, StringComparison.OrdinalIgnoreCase)
                && left.Value == right.Value;
        }

        private static double? ReadTotalValue(object instance)
        {
            return TryReadDouble(instance, "Total")
                ?? TryReadDouble(instance, "TotalValue")
                ?? TryReadDouble(instance, "Value")
                ?? TryReadDouble(instance, "HousingValue")
                ?? TryReadDouble(instance, "XP")
                ?? TryReadDouble(instance, "Experience");
        }

        private static IEnumerable<HousingPropertyRoomValue> ReadRoomValues(object value)
        {
            if (value == null || value is string)
            {
                yield break;
            }

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    var room = BuildRoomValue(entry.Key, entry.Value);
                    if (room != null)
                    {
                        yield return room;
                    }
                }

                yield break;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var entry in enumerable.Cast<object>().Take(30))
                {
                    var room = BuildRoomValue(null, entry);
                    if (room != null)
                    {
                        yield return room;
                    }
                }

                yield break;
            }

            var singleRoom = BuildRoomValue(null, value);
            if (singleRoom != null)
            {
                yield return singleRoom;
            }
        }

        private static HousingPropertyRoomValue BuildRoomValue(object key, object value)
        {
            var roomSource = UnwrapKeyValuePair(value, out var pairKey, out var pairValue)
                ? pairValue
                : value;
            var categorySource = key ?? pairKey ?? roomSource;
            var roomValue = ReadMember(roomSource, "RoomValue");
            var roomStats = ReadMember(roomSource, "RoomStats");
            var category = NormalizeCategory(ReadRoomCategory(roomValue)
                ?? ReadDisplayString(categorySource)
                ?? TryReadString(roomSource, "Category")
                ?? TryReadString(roomSource, "RoomCategory")
                ?? TryReadString(roomSource, "BestRoomCategory")
                ?? TryReadString(roomValue, "Title"));

            var roomName = NormalizeCategory(
                TryReadString(roomSource, "Name")
                ?? TryReadString(roomSource, "DisplayName")
                ?? category);
            var valueNumber = TryReadDouble(roomValue, "Value")
                ?? TryReadDouble(roomSource, "Value")
                ?? TryReadDouble(roomSource, "Total")
                ?? TryReadDouble(roomSource, "TotalValue")
                ?? TryReadDouble(roomSource, "HousingValue")
                ?? TryReadDirectDouble(roomSource);
            var tierObject = ReadMember(roomValue, "Tier");
            var tier = ReadTierValue(tierObject)
                ?? TryReadDouble(roomStats, "AverageTier")
                ?? TryReadDouble(roomSource, "Tier")
                ?? TryReadDouble(roomSource, "MaterialTier")
                ?? TryReadDouble(roomSource, "RoomTier")
                ?? TryReadDouble(roomSource, "AverageTier")
                ?? TryReadDouble(roomSource, "AvgTier");
            var existingTypes = ReadExistingTypeCounts(roomSource);
            var categoryValues = ReadCategoryValues(roomSource);
            var objects = ReadRoomObjects(roomSource).ToArray();
            if (existingTypes.Count == 0 && objects.Length > 0)
            {
                existingTypes = objects
                    .Where(item => !string.IsNullOrWhiteSpace(item.TypeForRoomLimit))
                    .GroupBy(item => item.TypeForRoomLimit, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            }

            if (categoryValues.Count == 0 && objects.Length > 0)
            {
                categoryValues = objects
                    .Where(item => !string.IsNullOrWhiteSpace(item.Category))
                    .GroupBy(item => item.Category, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Sum(item => item.EstimatedContribution ?? item.BaseValue ?? 0), StringComparer.OrdinalIgnoreCase);
            }

            if (string.IsNullOrWhiteSpace(roomName) && string.IsNullOrWhiteSpace(category) && valueNumber == null)
            {
                return null;
            }

            return new HousingPropertyRoomValue(
                string.IsNullOrWhiteSpace(roomName) ? category ?? "Room" : roomName,
                string.IsNullOrWhiteSpace(category) ? roomName ?? "Unknown" : category,
                valueNumber,
                tier,
                existingTypes,
                categoryValues,
                objects,
                ReadDisplayString(ReadMember(roomValue, "Description")));
        }

        private static IReadOnlyDictionary<string, double> ReadCategoryValues(object roomSource)
        {
            var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ReadFurnitureLikeObjects(roomSource).Take(80))
            {
                var homeValue = ReadHomeValue(item);
                var category = NormalizeCategory(ReadDisplayString(ReadMember(homeValue, "Category")));
                var value = TryReadDouble(item, "FurnishingValue")
                    ?? TryReadDouble(homeValue, "BaseValue");
                if (string.IsNullOrWhiteSpace(category) || value == null)
                {
                    continue;
                }

                values[category] = values.TryGetValue(category, out var existing) ? existing + value.Value : value.Value;
            }

            return values;
        }

        private static IReadOnlyDictionary<string, int> ReadExistingTypeCounts(object roomSource)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var item in ReadFurnitureLikeObjects(roomSource).Take(80))
            {
                if (item != null && !seen.Add(item))
                {
                    continue;
                }

                var typeLimit = ReadTypeForRoomLimit(item);
                if (string.IsNullOrWhiteSpace(typeLimit))
                {
                    continue;
                }

                counts[typeLimit] = counts.TryGetValue(typeLimit, out var count) ? count + 1 : 1;
            }

            return counts;
        }

        private static IEnumerable<HousingPropertyRoomObjectValue> ReadRoomObjects(object roomSource)
        {
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in ReadFurnitureLikeObjects(roomSource).Take(120))
            {
                if (item == null || !seen.Add(item))
                {
                    continue;
                }

                var homeValue = ReadHomeValue(item);
                if (homeValue == null)
                {
                    continue;
                }

                var typeLimit = ReadTypeForRoomLimit(item);
                var count = string.IsNullOrWhiteSpace(typeLimit)
                    ? 0
                    : typeCounts.TryGetValue(typeLimit, out var existing) ? existing : 0;
                if (!string.IsNullOrWhiteSpace(typeLimit))
                {
                    typeCounts[typeLimit] = count + 1;
                }

                var duplicateMultiplier = TryReadDouble(homeValue, "DiminishingReturnMultiplier");
                var baseValue = TryReadDouble(item, "FurnishingValue")
                    ?? TryReadDouble(homeValue, "BaseValue");
                double? contribution = baseValue == null
                    ? (double?)null
                    : baseValue.Value * Math.Pow(duplicateMultiplier ?? 1, count);

                yield return new HousingPropertyRoomObjectValue(
                    ReadObjectDisplayName(item),
                    ReadObjectItemTypeName(item),
                    NormalizeCategory(ReadDisplayString(ReadMember(homeValue, "Category"))),
                    typeLimit,
                    baseValue,
                    duplicateMultiplier,
                    contribution,
                    true);
            }
        }

        private static IEnumerable<object> ReadFurnitureLikeObjects(object roomSource)
        {
            var roomStats = ReadMember(roomSource, "RoomStats");
            foreach (var item in InvokeContainedComponents(roomStats, "HousingComponent"))
            {
                yield return item;
            }

            foreach (var item in Enumerate(ReadMember(roomStats, "ContainedWorldObjects")))
            {
                yield return item;
            }

            foreach (var item in Enumerate(ReadMember(roomStats, "ContainedAndTouchingWorldObjects")))
            {
                yield return item;
            }

            foreach (var item in Enumerate(ReadMember(roomSource, "ContainedWorldObjects")))
            {
                yield return item;
            }

            foreach (var member in ReadMembers(roomSource))
            {
                if (!LooksLikeFurnitureCollection(member.Name))
                {
                    continue;
                }

                foreach (var item in Enumerate(member.Value))
                {
                    yield return item;
                }
            }
        }

        private static bool LooksLikeFurnitureCollection(string memberName)
        {
            return memberName.IndexOf("Furniture", StringComparison.OrdinalIgnoreCase) >= 0
                || memberName.IndexOf("WorldObject", StringComparison.OrdinalIgnoreCase) >= 0
                || memberName.IndexOf("Contained", StringComparison.OrdinalIgnoreCase) >= 0
                || memberName.IndexOf("Object", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static IEnumerable<object> InvokeContainedComponents(object source, string componentTypeName)
        {
            if (source == null)
            {
                yield break;
            }

            var componentType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type => string.Equals(type.Name, componentTypeName, StringComparison.Ordinal));
            if (componentType == null)
            {
                yield break;
            }

            var method = source.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.Name == "ContainedComponents"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == 0);
            if (method == null)
            {
                yield break;
            }

            object result = null;
            try
            {
                result = method.MakeGenericMethod(componentType).Invoke(source, null);
            }
            catch
            {
            }

            foreach (var item in Enumerate(result))
            {
                yield return item;
            }
        }

        private static IEnumerable<object> Enumerate(object value)
        {
            if (value == null || value is string)
            {
                yield break;
            }

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    yield return entry.Value;
                }

                yield break;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var entry in enumerable.Cast<object>())
                {
                    yield return entry;
                }
            }
            else
            {
                yield return value;
            }
        }

        private static string ReadTypeForRoomLimit(object source)
        {
            if (source == null)
            {
                return null;
            }

            return TryReadString(source, "TypeForRoomLimit")
                ?? TryReadString(ReadHomeValue(source), "TypeForRoomLimit");
        }

        private static string ReadObjectDisplayName(object source)
        {
            return ReadDisplayString(source)
                ?? ReadDisplayString(ReadMember(source, "Parent"))
                ?? ReadDisplayString(ReadMember(source, "Item"))
                ?? ReadDisplayString(ReadMember(ReadMember(source, "Stack"), "Item"))
                ?? ReadDisplayString(ReadMember(ReadMember(source, "Parent"), "Item"))
                ?? source?.GetType().Name
                ?? "Furniture";
        }

        private static string ReadObjectItemTypeName(object source)
        {
            return ReadMember(ReadMember(source, "Item"), "GetType")?.ToString()
                ?? ReadMember(ReadMember(ReadMember(source, "Stack"), "Item"), "GetType")?.ToString()
                ?? source?.GetType().Name;
        }

        private static object ReadHomeValue(object source)
        {
            if (source == null)
            {
                return null;
            }

            return ReadMember(source, "HomeValue")
                ?? ReadMember(InvokeGetComponent(source, "HousingComponent"), "HomeValue")
                ?? ReadMember(ReadMember(source, "Object"), "HomeValue")
                ?? ReadMember(InvokeGetComponent(ReadMember(source, "Object"), "HousingComponent"), "HomeValue")
                ?? ReadMember(ReadMember(source, "Item"), "HomeValue")
                ?? ReadMember(InvokeGetComponent(ReadMember(source, "Parent"), "HousingComponent"), "HomeValue")
                ?? ReadMember(ReadMember(ReadMember(source, "Stack"), "Item"), "HomeValue")
                ?? ReadMember(ReadMember(source, "Parent"), "HomeValue");
        }

        private static object InvokeGetComponent(object source, string componentTypeName)
        {
            if (source == null)
            {
                return null;
            }

            var componentType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type => string.Equals(type.Name, componentTypeName, StringComparison.Ordinal));
            if (componentType == null)
            {
                return null;
            }

            var method = source.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.Name == "GetComponent"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == 0);
            if (method == null)
            {
                return null;
            }

            try
            {
                return method.MakeGenericMethod(componentType).Invoke(source, null);
            }
            catch
            {
                return null;
            }
        }

        private static bool UnwrapKeyValuePair(object value, out object key, out object pairValue)
        {
            key = null;
            pairValue = null;
            if (value == null)
            {
                return false;
            }

            var type = value.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>))
            {
                return false;
            }

            key = ReadMember(value, "Key");
            pairValue = ReadMember(value, "Value");
            return true;
        }

        private static string ReadDisplayString(object value)
        {
            if (value == null)
            {
                return null;
            }

            return TryReadString(value, "DisplayName")
                ?? TryReadString(value, "Name")
                ?? value.ToString();
        }

        private static string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return HousingRoomRules.NormalizeRoomName(value);
        }

        private static string ReadRoomCategory(object roomValue)
        {
            return ReadDisplayString(ReadMember(roomValue, "RoomCategory"))
                ?? ReadDisplayString(ReadMember(roomValue, "Category"))
                ?? ReadDisplayString(ReadMember(roomValue, "Title"))
                ?? TryReadString(roomValue, "RoomCategory")
                ?? TryReadString(roomValue, "Category");
        }

        private static double? ReadTierValue(object value)
        {
            return TryReadDouble(value, "TierVal")
                ?? TryReadDouble(value, "Value")
                ?? TryReadDouble(value, "Tier")
                ?? TryReadDirectDouble(value)
                ?? ExtractFirstNumber(value?.ToString());
        }

        private static double? ExtractFirstNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var chars = value
                .SkipWhile(ch => !char.IsDigit(ch) && ch != '-' && ch != '.')
                .TakeWhile(ch => char.IsDigit(ch) || ch == '-' || ch == '.' || ch == ',')
                .ToArray();
            if (chars.Length == 0)
            {
                return null;
            }

            var text = new string(chars).Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : (double?)null;
        }

        private static IEnumerable<(string Name, object Value)> ReadMembers(object instance)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = instance.GetType();
            foreach (var property in type.GetProperties(Flags).Where(property => property.GetIndexParameters().Length == 0))
            {
                object value = null;
                try { value = property.GetValue(instance); }
                catch { }

                yield return (property.Name, value);
            }

            foreach (var field in type.GetFields(Flags))
            {
                object value = null;
                try { value = field.GetValue(instance); }
                catch { }

                yield return (field.Name, value);
            }
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

        private static object ReadMember(object instance, string memberName)
        {
            if (instance == null)
            {
                return null;
            }

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var type = instance.GetType();
            var property = type.GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(instance);

            var field = type.GetField(memberName, Flags);
            return field == null ? null : field.GetValue(instance);
        }

        private static string TryReadString(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            return value?.ToString();
        }

        private static double? TryReadDouble(object instance, string memberName)
        {
            return TryReadDirectDouble(ReadMember(instance, memberName));
        }

        private static double? TryReadDirectDouble(object value)
        {
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static int? CountEnumerable(object value)
        {
            if (value == null || value is string)
            {
                return null;
            }

            if (value is ICollection collection)
            {
                return collection.Count;
            }

            if (value is IEnumerable enumerable)
            {
                var count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }

                return count;
            }

            return null;
        }

        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}

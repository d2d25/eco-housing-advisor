using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Eco.Gameplay.Players;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class EcoRoomDiagnosticsReader
    {
        public HousingRoomDiagnostics ReadCurrentRoom(User user)
        {
            var warnings = new List<string>();
            object room = null;
            try
            {
                Invoke(user, "UpdateRoom");
                room = ReadMember(user, "CurrentRoom");
            }
            catch (Exception exception)
            {
                warnings.Add("Unable to update/read CurrentRoom: " + exception.GetType().Name);
            }

            if (room == null)
            {
                warnings.Add("CurrentRoom is null. Stand inside the room you want to diagnose.");
                return new HousingRoomDiagnostics("No current room", "Unknown", null, null, Array.Empty<HousingRoomObjectDiagnostics>(), new Dictionary<string, int>(), warnings, DateTimeOffset.UtcNow);
            }

            var roomStats = ReadMember(room, "RoomStats");
            var roomValue = ReadMember(room, "RoomValue");
            var objects = Enumerate(ReadMember(roomStats, "ContainedWorldObjects"))
                .Concat(Enumerate(ReadMember(room, "ContainedWorldObjects")))
                .Concat(Enumerate(ReadMember(room, "WorldObjects")))
                .Where(item => item != null)
                .Distinct(ReferenceEqualityComparer.Instance)
                .Take(120)
                .Select(BuildObjectDiagnostics)
                .ToArray();

            var counts = objects
                .Where(item => !string.IsNullOrWhiteSpace(item.TypeForRoomLimit))
                .GroupBy(item => item.TypeForRoomLimit, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            if (objects.Length == 0)
            {
                warnings.Add("No contained world objects were readable from RoomStats/Room.");
            }

            if (objects.Length > 0 && counts.Count == 0)
            {
                warnings.Add("Objects were found, but no TypeForRoomLimit was readable from HousingComponent.HomeValue.");
            }

            return new HousingRoomDiagnostics(
                TryReadString(room, "Name") ?? TryReadString(room, "DisplayName") ?? room.GetType().Name,
                ReadCategory(room, roomStats, roomValue) ?? "Unknown",
                TryReadDouble(roomValue, "Value") ?? TryReadDouble(room, "Value"),
                TryReadDouble(ReadMember(roomValue, "Tier"), "TierVal") ?? TryReadDouble(roomStats, "AverageTier"),
                objects,
                counts,
                warnings,
                DateTimeOffset.UtcNow);
        }

        private static HousingRoomObjectDiagnostics BuildObjectDiagnostics(object worldObject)
        {
            var housingComponent = InvokeGetComponent(worldObject, "HousingComponent");
            var homeValue = ReadMember(housingComponent, "HomeValue")
                ?? ReadMember(worldObject, "HomeValue")
                ?? ReadMember(ReadMember(worldObject, "Item"), "HomeValue");

            return new HousingRoomObjectDiagnostics(
                worldObject.GetType().Name,
                TryReadString(worldObject, "DisplayName") ?? TryReadString(worldObject, "Name") ?? worldObject.GetType().Name,
                ReadDisplayString(ReadMember(homeValue, "Category")),
                TryReadString(homeValue, "TypeForRoomLimit"),
                TryReadDouble(homeValue, "BaseValue"),
                TryReadDouble(homeValue, "DiminishingReturnMultiplier") ?? TryReadDouble(homeValue, "DiminishingMultiplierAcrossFullProperty"),
                housingComponent != null);
        }

        private static string ReadCategory(params object[] sources)
        {
            foreach (var source in sources)
            {
                var category = ReadMember(source, "Category") ?? ReadMember(source, "RoomCategory");
                var text = ReadDisplayString(category)
                    ?? TryReadString(source, "Category")
                    ?? TryReadString(source, "RoomCategory")
                    ?? TryReadString(source, "BestRoomCategory");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return HousingRoomRules.NormalizeRoomName(text);
                }
            }

            return null;
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

        private static IEnumerable<object> Enumerate(object value)
        {
            if (value == null || value is string)
            {
                yield break;
            }

            if (value is IEnumerable enumerable)
            {
                foreach (var item in enumerable.Cast<object>())
                {
                    yield return item;
                }
            }
            else
            {
                yield return value;
            }
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
            var method = source.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.Name == "GetComponent"
                    && candidate.IsGenericMethodDefinition
                    && candidate.GetParameters().Length == 0);
            if (componentType == null || method == null)
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

        private static void Invoke(object instance, string memberName)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            instance.GetType().GetMethod(memberName, Flags)?.Invoke(instance, null);
        }

        private static object ReadMember(object instance, string memberName)
        {
            if (instance == null)
            {
                return null;
            }

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var property = instance.GetType().GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance);
            }

            var field = instance.GetType().GetField(memberName, Flags);
            return field?.GetValue(instance);
        }

        private static string TryReadString(object instance, string memberName)
        {
            return ReadMember(instance, memberName)?.ToString();
        }

        private static double? TryReadDouble(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
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

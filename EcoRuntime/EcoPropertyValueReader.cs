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
                return new HousingPropertyValueSnapshot("null", null, rooms, new[] { "PropertyValue was null." }, DateTimeOffset.UtcNow);
            }

            var totalValue = ReadTotalValue(propertyValue);
            foreach (var member in ReadMembers(propertyValue))
            {
                if (!LooksLikeRoomCollection(member.Name))
                {
                    continue;
                }

                foreach (var room in ReadRoomValues(member.Value))
                {
                    if (!rooms.Any(existing => SameRoom(existing, room)))
                    {
                        rooms.Add(room);
                    }
                }
            }

            if (rooms.Count == 0)
            {
                warnings.Add("PropertyValue room list was not readable yet; runtime member mapping needs confirmation on this Eco version.");
            }

            return new HousingPropertyValueSnapshot(
                propertyValue.GetType().FullName ?? propertyValue.GetType().Name,
                totalValue,
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

        private static bool LooksLikeRoomCollection(string memberName)
        {
            return memberName.IndexOf("Room", StringComparison.OrdinalIgnoreCase) >= 0
                || memberName.IndexOf("Category", StringComparison.OrdinalIgnoreCase) >= 0;
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
            var category = NormalizeCategory(ReadDisplayString(categorySource)
                ?? TryReadString(roomSource, "Category")
                ?? TryReadString(roomSource, "RoomCategory")
                ?? TryReadString(roomSource, "BestRoomCategory"));

            var roomName = NormalizeCategory(
                TryReadString(roomSource, "Name")
                ?? TryReadString(roomSource, "DisplayName")
                ?? category);
            var valueNumber = TryReadDouble(roomSource, "Value")
                ?? TryReadDouble(roomSource, "Total")
                ?? TryReadDouble(roomSource, "TotalValue")
                ?? TryReadDouble(roomSource, "HousingValue")
                ?? TryReadDirectDouble(roomSource);

            if (string.IsNullOrWhiteSpace(roomName) && string.IsNullOrWhiteSpace(category) && valueNumber == null)
            {
                return null;
            }

            return new HousingPropertyRoomValue(
                string.IsNullOrWhiteSpace(roomName) ? category ?? "Room" : roomName,
                string.IsNullOrWhiteSpace(category) ? roomName ?? "Unknown" : category,
                valueNumber);
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
    }
}

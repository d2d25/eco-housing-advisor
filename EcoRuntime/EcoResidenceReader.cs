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
    public sealed class EcoResidenceReader
    {
        public HousingResidenceSnapshot ReadResidence(User user)
        {
            var warnings = new List<string>();
            var rooms = new List<HousingRoomSnapshot>();

            object currentRoom = null;
            try
            {
                Invoke(user, "UpdateRoom");
                currentRoom = ReadMember(user, "CurrentRoom");
            }
            catch (Exception ex)
            {
                warnings.Add("Unable to update/read current room: " + ex.GetType().Name);
            }

            if (currentRoom != null)
            {
                rooms.Add(BuildRoomSnapshot(currentRoom, "current room"));
            }
            else
            {
                warnings.Add("Current room was not available. Stand inside a finished room for the first probe.");
            }

            foreach (var room in FindRoomLikeObjects(user).Take(12))
            {
                if (room == null || ReferenceEquals(room, currentRoom) || rooms.Any(existing => ReferenceEquals(existing.Source, room)))
                {
                    continue;
                }

                var snapshot = BuildRoomSnapshot(room, "user graph probe");
                if (!rooms.Any(existing => SameRoom(existing, snapshot)))
                {
                    rooms.Add(snapshot);
                }
            }

            if (rooms.Count <= 1)
            {
                warnings.Add("Full residence room enumeration is not confirmed yet; V0.7 reports confirmed room data first.");
            }

            return new HousingResidenceSnapshot(
                TryReadString(user, "Name") ?? "Residence",
                rooms,
                warnings,
                DateTimeOffset.UtcNow);
        }

        private static bool SameRoom(HousingRoomSnapshot left, HousingRoomSnapshot right)
        {
            return string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Category, right.Category, StringComparison.OrdinalIgnoreCase)
                && left.CurrentValue == right.CurrentValue
                && left.Tier == right.Tier;
        }

        private static HousingRoomSnapshot BuildRoomSnapshot(object room, string source)
        {
            var roomStats = ReadMember(room, "RoomStats");
            var roomValue = ReadMember(room, "RoomValue");
            var category = ReadCategory(room, roomStats, roomValue);
            var tier = TryReadDouble(roomStats, "Tier")
                ?? TryReadDouble(roomStats, "MaterialTier")
                ?? TryReadDouble(roomStats, "RoomTier")
                ?? TryReadDouble(roomStats, "AverageTier")
                ?? TryReadDouble(roomStats, "AvgTier")
                ?? TryReadDouble(room, "Tier")
                ?? TryReadDouble(room, "MaterialTier");

            return new HousingRoomSnapshot(
                TryReadString(room, "Name") ?? category ?? "Room",
                category ?? "Unknown",
                tier,
                TryReadDouble(roomValue, "Value") ?? TryReadDouble(room, "Value"),
                CountEnumerable(ReadMember(roomStats, "ContainedWorldObjects")),
                TryReadBool(roomStats, "Contained") ?? false,
                source);
        }

        private static string ReadCategory(params object[] sources)
        {
            foreach (var source in sources)
            {
                var direct = TryReadString(source, "Category")
                    ?? TryReadString(source, "RoomCategory")
                    ?? TryReadString(source, "BestRoomCategory")
                    ?? TryReadString(source, "Type");
                if (!string.IsNullOrWhiteSpace(direct))
                {
                    return NormalizeCategory(direct);
                }

                var category = ReadMember(source, "Category") ?? ReadMember(source, "RoomCategory");
                var nested = TryReadString(category, "DisplayName")
                    ?? TryReadString(category, "Name")
                    ?? category?.ToString();
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return NormalizeCategory(nested);
                }
            }

            return null;
        }

        private static string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var cleaned = value.Trim();
            if (cleaned.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0) return "Bedroom";
            if (cleaned.IndexOf("Kitchen", StringComparison.OrdinalIgnoreCase) >= 0) return "Kitchen";
            if (cleaned.IndexOf("Bathroom", StringComparison.OrdinalIgnoreCase) >= 0) return "Bathroom";
            if (cleaned.IndexOf("Living Room", StringComparison.OrdinalIgnoreCase) >= 0) return "Living Room";
            if (cleaned.IndexOf("LivingRoom", StringComparison.OrdinalIgnoreCase) >= 0) return "Living Room";
            if (cleaned.IndexOf("Outdoor", StringComparison.OrdinalIgnoreCase) >= 0) return "Outdoor";
            if (cleaned.IndexOf("Cultural", StringComparison.OrdinalIgnoreCase) >= 0) return "Cultural";
            return cleaned;
        }

        private static IEnumerable<object> FindRoomLikeObjects(object root)
        {
            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            var queue = new Queue<object>();
            queue.Enqueue(root);

            for (var depth = 0; depth < 4 && queue.Count > 0; depth++)
            {
                var count = queue.Count;
                for (var i = 0; i < count; i++)
                {
                    var current = queue.Dequeue();
                    if (current == null || !seen.Add(current))
                    {
                        continue;
                    }

                    var typeName = current.GetType().Name;
                    if (typeName.IndexOf("Room", StringComparison.OrdinalIgnoreCase) >= 0
                        && ReadMember(current, "RoomStats") != null)
                    {
                        yield return current;
                    }

                    foreach (var child in ReadChildren(current).Take(40))
                    {
                        if (child != null && IsReasonableObject(child))
                        {
                            queue.Enqueue(child);
                        }
                    }
                }
            }
        }

        private static IEnumerable<object> ReadChildren(object instance)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;
            foreach (var property in instance.GetType().GetProperties(Flags).Where(p => p.GetIndexParameters().Length == 0))
            {
                object value = null;
                try { value = property.GetValue(instance); }
                catch { }

                if (value == null || value is string)
                {
                    continue;
                }

                var enumerable = value as IEnumerable;
                if (enumerable != null && !(value is IDictionary))
                {
                    foreach (var item in enumerable.Cast<object>().Take(30))
                    {
                        yield return item;
                    }
                }
                else
                {
                    yield return value;
                }
            }
        }

        private static bool IsReasonableObject(object value)
        {
            var type = value.GetType();
            return !type.IsPrimitive
                && type != typeof(decimal)
                && type != typeof(DateTime)
                && type.Namespace != null
                && type.Namespace.StartsWith("Eco.", StringComparison.Ordinal);
        }

        private static int? CountEnumerable(object value)
        {
            var enumerable = value as IEnumerable;
            if (enumerable == null || value is string)
            {
                return null;
            }

            var count = 0;
            foreach (var _ in enumerable)
            {
                count++;
            }

            return count;
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

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type = instance.GetType();
            var property = type.GetProperty(memberName, Flags);
            if (property != null && property.GetIndexParameters().Length == 0) return property.GetValue(instance);

            var field = type.GetField(memberName, Flags);
            return field == null ? null : field.GetValue(instance);
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

        private static bool? TryReadBool(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            return value is bool b ? b : (bool?)null;
        }

        private static string TryReadString(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            if (value == null)
            {
                return null;
            }

            return value.ToString();
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

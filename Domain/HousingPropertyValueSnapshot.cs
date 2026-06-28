using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingPropertyRoomValue
    {
        public HousingPropertyRoomValue(
            string roomName,
            string category,
            double? value,
            double? tier = null,
            IReadOnlyDictionary<string, int> existingTypeCounts = null)
        {
            this.RoomName = roomName;
            this.Category = category;
            this.Value = value;
            this.Tier = tier;
            this.ExistingTypeCounts = existingTypeCounts ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        public string RoomName { get; }

        public string Category { get; }

        public double? Value { get; }

        public double? Tier { get; }

        public IReadOnlyDictionary<string, int> ExistingTypeCounts { get; }

        public int CountExistingType(string typeForRoomLimit)
        {
            if (string.IsNullOrWhiteSpace(typeForRoomLimit))
            {
                return 0;
            }

            return this.ExistingTypeCounts.TryGetValue(typeForRoomLimit, out var count)
                ? count
                : 0;
        }

        public string FormatKnownExistingTypes()
        {
            return this.ExistingTypeCounts.Count == 0
                ? "none mapped"
                : string.Join(", ", this.ExistingTypeCounts.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => entry.Key + " x" + entry.Value));
        }
    }

    public sealed class HousingPropertyValueSnapshot
    {
        public HousingPropertyValueSnapshot(
            string sourceType,
            double? totalValue,
            IReadOnlyList<HousingPropertyRoomValue> rooms,
            IReadOnlyList<string> warnings,
            DateTimeOffset generatedAt)
        {
            this.SourceType = sourceType;
            this.TotalValue = totalValue;
            this.Rooms = rooms;
            this.Warnings = warnings;
            this.GeneratedAt = generatedAt;
        }

        public string SourceType { get; }

        public double? TotalValue { get; }

        public IReadOnlyList<HousingPropertyRoomValue> Rooms { get; }

        public IReadOnlyList<string> Warnings { get; }

        public DateTimeOffset GeneratedAt { get; }
    }
}

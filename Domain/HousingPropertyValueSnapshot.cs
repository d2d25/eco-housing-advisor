using System;
using System.Collections.Generic;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingPropertyRoomValue
    {
        public HousingPropertyRoomValue(string roomName, string category, double? value)
        {
            this.RoomName = roomName;
            this.Category = category;
            this.Value = value;
        }

        public string RoomName { get; }

        public string Category { get; }

        public double? Value { get; }
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

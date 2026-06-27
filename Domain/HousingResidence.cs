using System;
using System.Collections.Generic;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingRoomSnapshot
    {
        public HousingRoomSnapshot(
            string name,
            string category,
            double? tier,
            double? currentValue,
            int? furnitureCount,
            bool contained,
            string source)
        {
            this.Name = name;
            this.Category = category;
            this.Tier = tier;
            this.CurrentValue = currentValue;
            this.FurnitureCount = furnitureCount;
            this.Contained = contained;
            this.Source = source;
        }

        public string Name { get; }

        public string Category { get; }

        public double? Tier { get; }

        public double? CurrentValue { get; }

        public int? FurnitureCount { get; }

        public bool Contained { get; }

        public string Source { get; }
    }

    public sealed class HousingResidenceSnapshot
    {
        public HousingResidenceSnapshot(
            string residenceName,
            IReadOnlyList<HousingRoomSnapshot> rooms,
            IReadOnlyList<string> warnings,
            DateTimeOffset generatedAt)
        {
            this.ResidenceName = residenceName;
            this.Rooms = rooms;
            this.Warnings = warnings;
            this.GeneratedAt = generatedAt;
        }

        public string ResidenceName { get; }

        public IReadOnlyList<HousingRoomSnapshot> Rooms { get; }

        public IReadOnlyList<string> Warnings { get; }

        public DateTimeOffset GeneratedAt { get; }
    }
}

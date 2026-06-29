using System;
using System.Collections.Generic;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingRoomDiagnostics
    {
        public HousingRoomDiagnostics(
            string roomName,
            string roomType,
            double? roomValue,
            double? tier,
            IReadOnlyList<HousingRoomObjectDiagnostics> objects,
            IReadOnlyDictionary<string, int> typeCounts,
            IReadOnlyList<string> warnings,
            DateTimeOffset generatedAt)
        {
            this.RoomName = roomName;
            this.RoomType = roomType;
            this.RoomValue = roomValue;
            this.Tier = tier;
            this.Objects = objects;
            this.TypeCounts = typeCounts;
            this.Warnings = warnings;
            this.GeneratedAt = generatedAt;
        }

        public string RoomName { get; }

        public string RoomType { get; }

        public double? RoomValue { get; }

        public double? Tier { get; }

        public IReadOnlyList<HousingRoomObjectDiagnostics> Objects { get; }

        public IReadOnlyDictionary<string, int> TypeCounts { get; }

        public IReadOnlyList<string> Warnings { get; }

        public DateTimeOffset GeneratedAt { get; }
    }

    public sealed class HousingRoomObjectDiagnostics
    {
        public HousingRoomObjectDiagnostics(
            string objectType,
            string displayName,
            string category,
            string typeForRoomLimit,
            double? baseValue,
            double? duplicateMultiplier,
            bool hasHousingComponent)
        {
            this.ObjectType = objectType;
            this.DisplayName = displayName;
            this.Category = category;
            this.TypeForRoomLimit = typeForRoomLimit;
            this.BaseValue = baseValue;
            this.DuplicateMultiplier = duplicateMultiplier;
            this.HasHousingComponent = hasHousingComponent;
        }

        public string ObjectType { get; }

        public string DisplayName { get; }

        public string Category { get; }

        public string TypeForRoomLimit { get; }

        public double? BaseValue { get; }

        public double? DuplicateMultiplier { get; }

        public bool HasHousingComponent { get; }
    }
}

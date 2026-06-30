using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingPropertyRoomObjectValue
    {
        public HousingPropertyRoomObjectValue(
            string displayName,
            string itemTypeName,
            string category,
            string typeForRoomLimit,
            double? baseValue,
            double? duplicateMultiplier,
            double? estimatedContribution,
            bool estimated)
            : this(displayName, itemTypeName, category, typeForRoomLimit, baseValue, duplicateMultiplier, estimatedContribution, estimated, null)
        {
        }

        public HousingPropertyRoomObjectValue(
            string displayName,
            string itemTypeName,
            string category,
            string typeForRoomLimit,
            double? baseValue,
            double? duplicateMultiplier,
            double? estimatedContribution,
            bool estimated,
            HousingLinkTarget objectLink)
        {
            this.DisplayName = displayName;
            this.ItemTypeName = itemTypeName;
            this.Category = category;
            this.TypeForRoomLimit = typeForRoomLimit;
            this.BaseValue = baseValue;
            this.DuplicateMultiplier = duplicateMultiplier;
            this.EstimatedContribution = estimatedContribution;
            this.Estimated = estimated;
            this.ObjectLink = objectLink;
        }

        public string DisplayName { get; }

        public string ItemTypeName { get; }

        public string Category { get; }

        public string TypeForRoomLimit { get; }

        public double? BaseValue { get; }

        public double? DuplicateMultiplier { get; }

        public double? EstimatedContribution { get; }

        public bool Estimated { get; }

        public HousingLinkTarget ObjectLink { get; }
    }

    public sealed class HousingPropertyRoomValue
    {
        public HousingPropertyRoomValue(
            string roomName,
            string category,
            double? value,
            double? tier = null,
            IReadOnlyDictionary<string, int> existingTypeCounts = null,
            IReadOnlyDictionary<string, double> categoryValues = null,
            IReadOnlyList<HousingPropertyRoomObjectValue> objects = null,
            string ecoDescription = null)
            : this(roomName, category, value, tier, existingTypeCounts, categoryValues, objects, ecoDescription, HousingLinkTarget.RoomCategory(category ?? roomName))
        {
        }

        public HousingPropertyRoomValue(
            string roomName,
            string category,
            double? value,
            double? tier,
            IReadOnlyDictionary<string, int> existingTypeCounts,
            IReadOnlyDictionary<string, double> categoryValues,
            IReadOnlyList<HousingPropertyRoomObjectValue> objects,
            string ecoDescription,
            HousingLinkTarget roomCategoryLink)
        {
            this.RoomName = roomName;
            this.Category = category;
            this.Value = value;
            this.Tier = tier;
            this.ExistingTypeCounts = existingTypeCounts ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            this.CategoryValues = categoryValues ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            this.Objects = objects ?? Array.Empty<HousingPropertyRoomObjectValue>();
            this.EcoDescription = ecoDescription;
            this.RoomCategoryLink = roomCategoryLink;
        }

        public string RoomName { get; }

        public string Category { get; }

        public double? Value { get; }

        public double? Tier { get; }

        public IReadOnlyDictionary<string, int> ExistingTypeCounts { get; }

        public IReadOnlyDictionary<string, double> CategoryValues { get; }

        public IReadOnlyList<HousingPropertyRoomObjectValue> Objects { get; }

        public string EcoDescription { get; }

        public HousingLinkTarget RoomCategoryLink { get; }

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

        public double CategoryValue(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return 0;
            }

            return this.CategoryValues.TryGetValue(category, out var value)
                ? value
                : string.Equals(category, this.Category, StringComparison.OrdinalIgnoreCase) && this.Value != null
                    ? this.Value.Value
                    : 0;
        }
    }

    public sealed class HousingPropertyValueSnapshot
    {
        public HousingPropertyValueSnapshot(
            string sourceType,
            double? totalValue,
            double? finalMultiplier,
            int? residentCount,
            IReadOnlyList<HousingPropertyRoomValue> rooms,
            IReadOnlyList<string> warnings,
            DateTimeOffset generatedAt)
        {
            this.SourceType = sourceType;
            this.TotalValue = totalValue;
            this.FinalMultiplier = finalMultiplier;
            this.ResidentCount = residentCount;
            this.Rooms = rooms;
            this.Warnings = warnings;
            this.GeneratedAt = generatedAt;
        }

        public string SourceType { get; }

        public double? TotalValue { get; }

        public double? FinalMultiplier { get; }

        public int? ResidentCount { get; }

        public IReadOnlyList<HousingPropertyRoomValue> Rooms { get; }

        public IReadOnlyList<string> Warnings { get; }

        public DateTimeOffset GeneratedAt { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingRoomAdditionAdvice
    {
        public HousingRoomAdditionAdvice(
            string category,
            HousingFurnitureGroup group,
            double estimatedGain,
            string capNote,
            int existingTypeCount,
            double duplicateFactor,
            HousingItemAvailability availability)
        {
            this.Category = category;
            this.Group = group;
            this.EstimatedGain = estimatedGain;
            this.CapNote = capNote;
            this.ExistingTypeCount = existingTypeCount;
            this.DuplicateFactor = duplicateFactor;
            this.Availability = availability;
        }

        public string Category { get; }

        public HousingFurnitureGroup Group { get; }

        public double EstimatedGain { get; }

        public string CapNote { get; }

        public int ExistingTypeCount { get; }

        public double DuplicateFactor { get; }

        public HousingItemAvailability Availability { get; }
    }

    public sealed class HousingRoomAdvice
    {
        public HousingRoomAdvice(HousingPropertyRoomValue room, IReadOnlyList<HousingRoomAdditionAdvice> additions)
        {
            this.Room = room;
            this.Additions = additions;
        }

        public HousingPropertyRoomValue Room { get; }

        public IReadOnlyList<HousingRoomAdditionAdvice> Additions { get; }
    }

    public sealed class HousingPropertyAdvice
    {
        public HousingPropertyAdvice(IReadOnlyList<HousingRoomAdvice> rooms)
            : this(rooms, new HousingRoomAdvice[0])
        {
        }

        public HousingPropertyAdvice(IReadOnlyList<HousingRoomAdvice> rooms, IReadOnlyList<HousingRoomAdvice> newRooms)
        {
            this.Rooms = rooms;
            this.NewRooms = newRooms;
        }

        public IReadOnlyList<HousingRoomAdvice> Rooms { get; }

        public IReadOnlyList<HousingRoomAdvice> NewRooms { get; }
    }

    public sealed class HousingPropertyAdviceEngine
    {
        public HousingPropertyAdvice BuildAdvice(
            HousingPropertyValueSnapshot property,
            IReadOnlyList<HousingFurnitureGroup> groups,
            HousingAvailabilitySnapshot availability,
            int maxRooms,
            int maxAdditionsPerRoom)
        {
            var indexedGroups = groups
                .GroupBy(group => group.Category, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(item => item.BaseValue).ThenBy(item => item.Items[0].DisplayName, StringComparer.OrdinalIgnoreCase).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            var roomAdvice = property.Rooms
                .OrderBy(room => room.Value ?? double.MaxValue)
                .ThenBy(room => room.RoomName, StringComparer.OrdinalIgnoreCase)
                .Take(maxRooms < 1 ? 2 : maxRooms)
                .Select(room => new HousingRoomAdvice(room, BuildAdditions(property, room, indexedGroups, availability, maxAdditionsPerRoom)))
                .Where(advice => advice.Additions.Count > 0)
                .ToArray();

            var newRoomAdvice = BuildNewRoomSetups(property, indexedGroups, availability, maxRooms, maxAdditionsPerRoom);

            return new HousingPropertyAdvice(roomAdvice, newRoomAdvice);
        }

        private static IReadOnlyList<HousingRoomAdvice> BuildNewRoomSetups(
            HousingPropertyValueSnapshot property,
            IReadOnlyDictionary<string, HousingFurnitureGroup[]> indexedGroups,
            HousingAvailabilitySnapshot availability,
            int maxRooms,
            int maxAdditionsPerRoom)
        {
            var limit = maxRooms < 1 ? 2 : maxRooms;
            var existingRooms = new HashSet<string>(
                property.Rooms
                    .Select(room => HousingRoomRules.NormalizeRoomName(room.Category ?? room.RoomName))
                    .Where(room => !string.IsNullOrWhiteSpace(room)),
                StringComparer.OrdinalIgnoreCase);

            return HousingRoomRules.StarterRoomPriority()
                .Where(roomCategory => !existingRooms.Contains(roomCategory))
                .Select(roomCategory => new HousingPropertyRoomValue("New " + roomCategory, roomCategory, 0, null))
                .Select(room => new HousingRoomAdvice(room, BuildRoomSetupAdditions(property, room, indexedGroups, availability, maxAdditionsPerRoom)))
                .Where(advice => advice.Additions.Count > 0)
                .Take(limit)
                .ToArray();
        }

        private static IReadOnlyList<HousingRoomAdditionAdvice> BuildRoomSetupAdditions(
            HousingPropertyValueSnapshot property,
            HousingPropertyRoomValue room,
            IReadOnlyDictionary<string, HousingFurnitureGroup[]> indexedGroups,
            HousingAvailabilitySnapshot availability,
            int maxAdditions)
        {
            var limit = maxAdditions < 1 ? 3 : maxAdditions;
            if (!indexedGroups.TryGetValue(room.Category, out var matches))
            {
                return Array.Empty<HousingRoomAdditionAdvice>();
            }

            return matches
                .Select(match => BuildAdditionAdvice(property, room, room.Category, match, availability.ForItem(match.Items[0].ItemTypeName)))
                .Where(advice => advice != null && advice.EstimatedGain > 0 && advice.Availability.IsAvailable)
                .OrderByDescending(advice => advice.EstimatedGain)
                .ThenBy(advice => advice.Group.Items[0].DisplayName, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
        }

        private static IReadOnlyList<HousingRoomAdditionAdvice> BuildAdditions(
            HousingPropertyValueSnapshot property,
            HousingPropertyRoomValue room,
            IReadOnlyDictionary<string, HousingFurnitureGroup[]> indexedGroups,
            HousingAvailabilitySnapshot availability,
            int maxAdditions)
        {
            var limit = maxAdditions < 1 ? 3 : maxAdditions;
            var categories = HousingRoomRules.CategoriesUsefulInRoom(room.Category ?? room.RoomName);
            return categories
                .SelectMany(category => indexedGroups.TryGetValue(category, out var matches)
                    ? matches.Select(match => BuildAdditionAdvice(property, room, category, match, availability.ForItem(match.Items[0].ItemTypeName)))
                    : Enumerable.Empty<HousingRoomAdditionAdvice>())
                .Where(advice => advice != null && advice.EstimatedGain > 0 && advice.Availability.IsAvailable)
                .OrderByDescending(advice => advice.EstimatedGain)
                .ThenBy(advice => advice.ExistingTypeCount)
                .ThenBy(advice => advice.Category, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
        }

        private static HousingRoomAdditionAdvice BuildAdditionAdvice(
            HousingPropertyValueSnapshot property,
            HousingPropertyRoomValue room,
            string category,
            HousingFurnitureGroup group,
            HousingItemAvailability availability)
        {
            var existingTypeCount = room.CountExistingType(group.TypeForRoomLimit);
            var duplicateFactor = DuplicateFactor(group, existingTypeCount);
            var duplicateAdjustedBase = ApplySupportCap(room, category, group.BaseValue * duplicateFactor);
            duplicateAdjustedBase = ApplyPropertyCategoryCap(property, room, duplicateAdjustedBase);
            var cap = HousingTierCaps.ForTier(room.Tier);
            if (cap == null || room.Value == null)
            {
                return new HousingRoomAdditionAdvice(category, group, duplicateAdjustedBase, "cap unknown", existingTypeCount, duplicateFactor, availability);
            }

            var remainingSoft = cap.SoftCap - room.Value.Value;
            var remainingHard = cap.HardCap - room.Value.Value;
            remainingHard = ApplyPropertyCategoryCap(property, room, remainingHard);
            if (remainingHard <= 0)
            {
                return new HousingRoomAdditionAdvice(category, group, 0, "hard cap reached", existingTypeCount, duplicateFactor, availability);
            }

            if (remainingSoft <= 0)
            {
                return new HousingRoomAdditionAdvice(
                    category,
                    group,
                    Math.Min(duplicateAdjustedBase * cap.DiminishingReturnPercent, remainingHard),
                    "past soft cap",
                    existingTypeCount,
                    duplicateFactor,
                    availability);
            }

            return new HousingRoomAdditionAdvice(
                category,
                group,
                Math.Min(duplicateAdjustedBase, Math.Min(remainingSoft, remainingHard)),
                "before soft cap",
                existingTypeCount,
                duplicateFactor,
                availability);
        }

        private static double DuplicateFactor(HousingFurnitureGroup group, int existingTypeCount)
        {
            if (existingTypeCount <= 0)
            {
                return 1;
            }

            var multiplier = group.DiminishingReturnMultiplier ?? 1;
            return Math.Pow(multiplier, existingTypeCount);
        }

        private static double ApplySupportCap(HousingPropertyRoomValue room, string candidateCategory, double candidateValue)
        {
            var primary = HousingRoomRules.NormalizeRoomName(room.Category ?? room.RoomName);
            if (string.Equals(candidateCategory, primary, StringComparison.OrdinalIgnoreCase))
            {
                return candidateValue;
            }

            var primaryValue = room.CategoryValue(primary);
            if (primaryValue <= 0)
            {
                return candidateValue;
            }

            var cap = HousingRoomRules.SupportCapPercent(candidateCategory, primary) * primaryValue;
            var existingSupport = room.CategoryValue(candidateCategory);
            return Math.Min(candidateValue, Math.Max(0, cap - existingSupport));
        }

        private static double ApplyPropertyCategoryCap(HousingPropertyValueSnapshot property, HousingPropertyRoomValue room, double remainingHard)
        {
            var roomCategory = HousingRoomRules.NormalizeRoomName(room.Category ?? room.RoomName);
            var capPercent = HousingRoomRules.PropertyCategoryCapPercent(roomCategory);
            if (capPercent <= 0)
            {
                return remainingHard;
            }

            var uncappedTotal = property.Rooms
                .Where(other => HousingRoomRules.PropertyCategoryCapPercent(other.Category) <= 0)
                .Sum(other => other.Value ?? 0);
            var currentCategoryTotal = property.Rooms
                .Where(other => string.Equals(HousingRoomRules.NormalizeRoomName(other.Category), roomCategory, StringComparison.OrdinalIgnoreCase))
                .Sum(other => other.Value ?? 0);
            return Math.Min(remainingHard, Math.Max(0, capPercent * uncappedTotal - currentCategoryTotal));
        }
    }
}

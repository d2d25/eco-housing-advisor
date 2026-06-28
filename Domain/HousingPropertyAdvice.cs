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
            double duplicateFactor)
        {
            this.Category = category;
            this.Group = group;
            this.EstimatedGain = estimatedGain;
            this.CapNote = capNote;
            this.ExistingTypeCount = existingTypeCount;
            this.DuplicateFactor = duplicateFactor;
        }

        public string Category { get; }

        public HousingFurnitureGroup Group { get; }

        public double EstimatedGain { get; }

        public string CapNote { get; }

        public int ExistingTypeCount { get; }

        public double DuplicateFactor { get; }
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
        {
            this.Rooms = rooms;
        }

        public IReadOnlyList<HousingRoomAdvice> Rooms { get; }
    }

    public sealed class HousingPropertyAdviceEngine
    {
        public HousingPropertyAdvice BuildAdvice(
            HousingPropertyValueSnapshot property,
            IReadOnlyList<HousingFurnitureGroup> groups,
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
                .Select(room => new HousingRoomAdvice(room, BuildAdditions(room, indexedGroups, maxAdditionsPerRoom)))
                .Where(advice => advice.Additions.Count > 0)
                .ToArray();

            return new HousingPropertyAdvice(roomAdvice);
        }

        private static IReadOnlyList<HousingRoomAdditionAdvice> BuildAdditions(
            HousingPropertyRoomValue room,
            IReadOnlyDictionary<string, HousingFurnitureGroup[]> indexedGroups,
            int maxAdditions)
        {
            var limit = maxAdditions < 1 ? 3 : maxAdditions;
            var categories = HousingRoomRules.CategoriesUsefulInRoom(room.Category ?? room.RoomName);
            return categories
                .SelectMany(category => indexedGroups.TryGetValue(category, out var matches)
                    ? matches.Select(match => BuildAdditionAdvice(room, category, match))
                    : Enumerable.Empty<HousingRoomAdditionAdvice>())
                .Where(advice => advice != null)
                .OrderByDescending(advice => advice.EstimatedGain)
                .ThenBy(advice => advice.ExistingTypeCount)
                .ThenBy(advice => advice.Category, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
        }

        private static HousingRoomAdditionAdvice BuildAdditionAdvice(HousingPropertyRoomValue room, string category, HousingFurnitureGroup group)
        {
            var existingTypeCount = room.CountExistingType(group.TypeForRoomLimit);
            var duplicateFactor = DuplicateFactor(group, existingTypeCount);
            var duplicateAdjustedBase = group.BaseValue * duplicateFactor;
            var cap = HousingTierCaps.ForTier(room.Tier);
            if (cap == null || room.Value == null)
            {
                return new HousingRoomAdditionAdvice(category, group, duplicateAdjustedBase, "cap unknown", existingTypeCount, duplicateFactor);
            }

            var remainingSoft = cap.SoftCap - room.Value.Value;
            var remainingHard = cap.HardCap - room.Value.Value;
            if (remainingHard <= 0)
            {
                return new HousingRoomAdditionAdvice(category, group, 0, "hard cap reached", existingTypeCount, duplicateFactor);
            }

            if (remainingSoft <= 0)
            {
                return new HousingRoomAdditionAdvice(
                    category,
                    group,
                    Math.Min(duplicateAdjustedBase * cap.DiminishingReturnPercent, remainingHard),
                    "past soft cap",
                    existingTypeCount,
                    duplicateFactor);
            }

            return new HousingRoomAdditionAdvice(
                category,
                group,
                Math.Min(duplicateAdjustedBase, remainingSoft),
                "before soft cap",
                existingTypeCount,
                duplicateFactor);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingRoomAdditionAdvice
    {
        public HousingRoomAdditionAdvice(string category, HousingFurnitureGroup group, double estimatedGain, string capNote)
        {
            this.Category = category;
            this.Group = group;
            this.EstimatedGain = estimatedGain;
            this.CapNote = capNote;
        }

        public string Category { get; }

        public HousingFurnitureGroup Group { get; }

        public double EstimatedGain { get; }

        public string CapNote { get; }
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
                .Select(category => indexedGroups.TryGetValue(category, out var matches) && matches.Length > 0
                    ? BuildAdditionAdvice(room, category, matches[0])
                    : null)
                .Where(advice => advice != null)
                .OrderByDescending(advice => advice.EstimatedGain)
                .ThenBy(advice => advice.Category, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
        }

        private static HousingRoomAdditionAdvice BuildAdditionAdvice(HousingPropertyRoomValue room, string category, HousingFurnitureGroup group)
        {
            var cap = HousingTierCaps.ForTier(room.Tier);
            if (cap == null || room.Value == null)
            {
                return new HousingRoomAdditionAdvice(category, group, group.BaseValue, "cap unknown");
            }

            var remainingSoft = cap.SoftCap - room.Value.Value;
            var remainingHard = cap.HardCap - room.Value.Value;
            if (remainingHard <= 0)
            {
                return new HousingRoomAdditionAdvice(category, group, 0, "hard cap reached");
            }

            if (remainingSoft <= 0)
            {
                return new HousingRoomAdditionAdvice(
                    category,
                    group,
                    Math.Min(group.BaseValue * cap.DiminishingReturnPercent, remainingHard),
                    "past soft cap");
            }

            return new HousingRoomAdditionAdvice(
                category,
                group,
                Math.Min(group.BaseValue, remainingSoft),
                "before soft cap");
        }
    }
}

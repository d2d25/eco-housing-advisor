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
            var localRoomGain = ApplySupportCap(room, category, group.BaseValue * duplicateFactor);
            var cap = HousingTierCaps.ForTier(room.Tier);
            if (cap == null || room.Value == null)
            {
                var uncappedTotalGain = EstimatePropertyDelta(property, room, category, localRoomGain);
                return new HousingRoomAdditionAdvice(category, group, uncappedTotalGain, "cap unknown", existingTypeCount, duplicateFactor, availability);
            }

            var remainingSoft = cap.SoftCap - room.Value.Value;
            var remainingHard = cap.HardCap - room.Value.Value;
            if (remainingHard <= 0)
            {
                return new HousingRoomAdditionAdvice(category, group, 0, "hard cap reached", existingTypeCount, duplicateFactor, availability);
            }

            double roomGain;
            string capNote;
            if (remainingSoft <= 0)
            {
                roomGain = Math.Min(localRoomGain * cap.DiminishingReturnPercent, remainingHard);
                capNote = "past soft cap";
            }
            else
            {
                roomGain = Math.Min(localRoomGain, Math.Min(remainingSoft, remainingHard));
                capNote = "before soft cap";
            }

            return new HousingRoomAdditionAdvice(
                category,
                group,
                EstimatePropertyDelta(property, room, category, roomGain),
                capNote,
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

        private static double EstimatePropertyDelta(
            HousingPropertyValueSnapshot property,
            HousingPropertyRoomValue targetRoom,
            string category,
            double roomGain)
        {
            if (roomGain <= 0)
            {
                return 0;
            }

            var before = EstimateFinalPropertyValue(property);
            var afterRooms = RoomsWithAdditionalValue(property, targetRoom, category, roomGain);
            var after = EstimateFinalPropertyValue(property, afterRooms);
            return Math.Round(Math.Max(0, after - before), 6);
        }

        private static IReadOnlyList<HousingPropertyRoomValue> RoomsWithAdditionalValue(
            HousingPropertyValueSnapshot property,
            HousingPropertyRoomValue targetRoom,
            string category,
            double roomGain)
        {
            var updated = false;
            var rooms = new List<HousingPropertyRoomValue>(property.Rooms.Count + 1);
            foreach (var room in property.Rooms)
            {
                if (ReferenceEquals(room, targetRoom))
                {
                    rooms.Add(CopyRoomWithAdditionalCategoryValue(room, category, roomGain));
                    updated = true;
                }
                else
                {
                    rooms.Add(room);
                }
            }

            if (!updated)
            {
                rooms.Add(CopyRoomWithAdditionalCategoryValue(targetRoom, category, roomGain));
            }

            return rooms;
        }

        private static HousingPropertyRoomValue CopyRoomWithAdditionalCategoryValue(
            HousingPropertyRoomValue room,
            string category,
            double roomGain)
        {
            if (room.CategoryValues.Count == 0 || string.IsNullOrWhiteSpace(category))
            {
                return CopyRoomWithValueAndCategories(room, (room.Value ?? 0) + roomGain, room.CategoryValues);
            }

            var values = new Dictionary<string, double>(room.CategoryValues, StringComparer.OrdinalIgnoreCase);
            values[category] = values.TryGetValue(category, out var existing)
                ? existing + roomGain
                : roomGain;
            return CopyRoomWithValueAndCategories(room, EstimateRoomFinalValue(room, values), values);
        }

        private static HousingPropertyRoomValue CopyRoomWithValueAndCategories(
            HousingPropertyRoomValue room,
            double value,
            IReadOnlyDictionary<string, double> categoryValues)
        {
            return new HousingPropertyRoomValue(
                room.RoomName,
                room.Category,
                value,
                room.Tier,
                room.ExistingTypeCounts,
                categoryValues,
                room.Objects,
                room.EcoDescription,
                room.RoomCategoryLink);
        }

        private static double EstimateFinalPropertyValue(HousingPropertyValueSnapshot property)
        {
            return EstimateFinalPropertyValue(property, property.Rooms);
        }

        private static double EstimateFinalPropertyValue(
            HousingPropertyValueSnapshot property,
            IReadOnlyList<HousingPropertyRoomValue> rooms)
        {
            var adjustedRooms = EstimateRoomsAfterDuplicateRoomPenalty(rooms, property.ResidentCount);
            var uncappedTotal = adjustedRooms
                .Where(room => HousingRoomRules.PropertyCategoryCapPercent(room.Category) <= 0)
                .Sum(room => room.Value);
            var cappedTotal = adjustedRooms
                .Where(room => HousingRoomRules.PropertyCategoryCapPercent(room.Category) > 0)
                .GroupBy(room => HousingRoomRules.NormalizeRoomName(room.Category), StringComparer.OrdinalIgnoreCase)
                .Sum(group =>
                {
                    var category = group.Key;
                    var rawValue = group.Sum(room => room.Value);
                    var cap = HousingRoomRules.PropertyCategoryCapPercent(category);
                    return Math.Min(rawValue, cap * uncappedTotal);
                });

            return uncappedTotal + cappedTotal;
        }

        private static IReadOnlyList<EstimatedRoomValue> EstimateRoomsAfterDuplicateRoomPenalty(
            IReadOnlyList<HousingPropertyRoomValue> rooms,
            int? residentCount)
        {
            var fullRoomsPerCategory = Math.Max(1, residentCount ?? 1);
            return rooms
                .Select(room => new EstimatedRoomValue(
                    HousingRoomRules.NormalizeRoomName(room.Category ?? room.RoomName),
                    EstimateRoomFinalValue(room)))
                .GroupBy(room => room.Category, StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => group
                    .OrderByDescending(room => room.Value)
                    .Select((room, index) => index < fullRoomsPerCategory
                        ? room
                        : new EstimatedRoomValue(room.Category, room.Value * 0.1)))
                .ToArray();
        }

        private static double EstimateRoomFinalValue(HousingPropertyRoomValue room)
        {
            return EstimateRoomFinalValue(room, room.CategoryValues);
        }

        private static double EstimateRoomFinalValue(
            HousingPropertyRoomValue room,
            IReadOnlyDictionary<string, double> categoryValues)
        {
            if (categoryValues == null || categoryValues.Count == 0)
            {
                return room.Value ?? 0;
            }

            var primary = HousingRoomRules.NormalizeRoomName(room.Category ?? room.RoomName);
            var primaryValue = categoryValues.TryGetValue(primary, out var rawPrimary)
                ? rawPrimary
                : room.Value ?? 0;
            var supportValue = categoryValues
                .Where(entry => !string.Equals(HousingRoomRules.NormalizeRoomName(entry.Key), primary, StringComparison.OrdinalIgnoreCase))
                .Sum(entry =>
                {
                    var supportCategory = HousingRoomRules.NormalizeRoomName(entry.Key);
                    var cap = HousingRoomRules.SupportCapPercent(supportCategory, primary) * primaryValue;
                    return Math.Min(entry.Value, cap);
                });

            return primaryValue + supportValue;
        }

        private sealed class EstimatedRoomValue
        {
            public EstimatedRoomValue(string category, double value)
            {
                this.Category = category;
                this.Value = value;
            }

            public string Category { get; }

            public double Value { get; }
        }

    }
}

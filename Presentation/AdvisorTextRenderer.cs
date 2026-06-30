using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.EcoRuntime;

namespace EcoHousingAdvisor.Presentation
{
    public sealed class AdvisorTextRenderer
    {
        public string RenderFurnitureResult(HousingFurnitureQueryResult result)
        {
            if (result.Groups.Count == 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: no matching furniture. {0}. Total: {1} entries in {2} groups.",
                    result.Message,
                    result.TotalFurniture,
                    result.TotalGroups);
            }

            var lines = new List<string>
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: {0}. Showing {1}/{2} groups, page {3}/{4}. Total: {5} entries in {6} groups.",
                    result.Message,
                    result.Groups.Count,
                    result.FilteredGroupCount,
                    result.Query.Page,
                    result.PageCount,
                    result.TotalFurniture,
                    result.TotalGroups),
            };

            AddGroupLines(lines, result.Groups);

            if (result.PageCount > 1)
            {
                lines.Add(NextPageHint(result));
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string RenderFurnitureGroups(IReadOnlyList<HousingFurnitureGroup> groups)
        {
            if (groups.Count == 0)
            {
                return "Eco Housing Advisor: no housing furniture found.";
            }

            var lines = new List<string>
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: {0} furniture entries in {1} groups.",
                    groups.Sum(group => group.Items.Count),
                    groups.Count),
            };

            AddGroupLines(lines, groups);

            return string.Join(Environment.NewLine, lines);
        }

        public string RenderDebug(HousingFurnitureSnapshot snapshot)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Eco Housing Advisor debug: {0} furniture entries, {1} groups, generated {2:O}. Cache is active.",
                snapshot.Furniture.Count,
                snapshot.Groups.Count,
                snapshot.GeneratedAt);
        }

        public string RenderHelp()
        {
            return string.Join(Environment.NewLine, new[]
            {
                "Eco Housing Advisor commands:",
                "/housingadvisor suggest Seating - what to buy/craft for a category",
                "/housingadvisor list - first summary page",
                "/housingadvisor list 2 - summary page 2",
                "/housingadvisor category Seating - filter one category",
                "/housingadvisor search chair - search furniture",
                "/housingadvisor hadebug - cache/discovery debug",
                "/housingadvisor harefresh - rebuild furniture cache",
                "/housingadvisor harooms - list active residence rooms",
                "/housingadvisor haroom Bedroom - show furniture details for one room type",
                "/housingadvisor uistatus - show UI probe status",
                "/housingadvisor hahelp - show this help",
            });
        }

        public string RenderRooms(HousingPropertyValueSnapshot snapshot)
        {
            var lines = new List<string>
            {
                "Eco Housing Advisor: active residence rooms.",
            };

            if (snapshot.Rooms.Count == 0)
            {
                lines.Add("No active residence rooms were readable from Eco.");
                foreach (var warning in snapshot.Warnings)
                {
                    lines.Add("Note: " + warning);
                }

                return string.Join(Environment.NewLine, lines);
            }

            var roomSum = snapshot.Rooms.Sum(room => room.Value ?? 0);
            lines.Add(string.Format(
                CultureInfo.InvariantCulture,
                "Total Eco: {0} XP/day. Rooms sum: {1} XP/day.",
                FormatNullable(snapshot.TotalValue),
                HousingFurnitureFormatter.FormatBaseValue(roomSum)));

            foreach (var room in snapshot.Rooms.OrderBy(room => room.Category, StringComparer.OrdinalIgnoreCase).ThenBy(room => room.RoomName, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: type {1}, tier {2}, Eco {3} XP/day, objects {4}",
                    room.RoomName,
                    room.Category,
                    FormatNullable(room.Tier),
                    FormatNullable(room.Value),
                    room.Objects.Count));

                lines.Add("  types: " + room.FormatKnownExistingTypes());
            }

            foreach (var warning in snapshot.Warnings)
            {
                lines.Add("Note: " + warning);
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string RenderRoomDetails(HousingPropertyValueSnapshot snapshot, string roomType)
        {
            var normalized = HousingRoomRules.NormalizeRoomName(roomType);
            var rooms = snapshot.Rooms
                .Where(room => string.Equals(HousingRoomRules.NormalizeRoomName(room.Category), normalized, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(HousingRoomRules.NormalizeRoomName(room.RoomName), normalized, StringComparison.OrdinalIgnoreCase))
                .OrderBy(room => room.RoomName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (rooms.Length == 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: no active residence room found for '{0}'. Use /housingadvisor harooms to see readable rooms.",
                    roomType);
            }

            var lines = new List<string>
            {
                string.Format(CultureInfo.InvariantCulture, "Eco Housing Advisor: {0} room details.", normalized),
            };

            foreach (var room in rooms)
            {
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: Eco {1} XP/day, tier {2}, objects {3}",
                    room.RoomName,
                    FormatNullable(room.Value),
                    FormatNullable(room.Tier),
                    room.Objects.Count));

                if (!string.IsNullOrWhiteSpace(room.EcoDescription))
                {
                    lines.Add("Eco detail: " + CompactLocString(room.EcoDescription));
                }

                if (room.Objects.Count == 0)
                {
                    lines.Add("No furniture objects readable from this room.");
                }

                foreach (var item in room.Objects.OrderBy(item => item.TypeForRoomLimit, StringComparer.OrdinalIgnoreCase).ThenByDescending(item => item.EstimatedContribution ?? item.BaseValue ?? 0).Take(30))
                {
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "- {0}: category {1}, type {2}, base {3}, gives +{4} XP/day{5}",
                        item.DisplayName,
                        string.IsNullOrWhiteSpace(item.Category) ? "?" : item.Category,
                        string.IsNullOrWhiteSpace(item.TypeForRoomLimit) ? "?" : item.TypeForRoomLimit,
                        FormatNullable(item.BaseValue),
                        FormatNullable(item.EstimatedContribution),
                        item.Estimated ? " est." : string.Empty));
                }

                var calculated = room.Objects.Sum(item => item.EstimatedContribution ?? 0);
                if (room.Value != null && room.Objects.Count > 0)
                {
                    var delta = calculated - room.Value.Value;
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Check: calculated {0} vs Eco {1} XP/day, diff {2}.",
                        HousingFurnitureFormatter.FormatBaseValue(calculated),
                        FormatNullable(room.Value),
                        HousingFurnitureFormatter.FormatBaseValue(delta)));
                }

                foreach (var type in room.ExistingTypeCounts.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Take(12))
                {
                    var example = room.Objects.FirstOrDefault(item => string.Equals(item.TypeForRoomLimit, type.Key, StringComparison.OrdinalIgnoreCase));
                    if (example?.DuplicateMultiplier == null)
                    {
                        continue;
                    }

                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "Next {0}: duplicate multiplier {1}, existing x{2}.",
                        type.Key,
                        HousingFurnitureFormatter.FormatMultiplier(Math.Pow(example.DuplicateMultiplier.Value, type.Value)),
                        type.Value));
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string RenderResidence(HousingResidenceSnapshot snapshot)
        {
            var lines = new List<string>
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: residence probe for {0}. Rooms seen: {1}.",
                    snapshot.ResidenceName,
                    snapshot.Rooms.Count),
            };

            foreach (var room in snapshot.Rooms)
            {
                var cap = HousingTierCaps.ForTier(room.Tier);
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: category {1}, tier {2}, value {3}, furniture {4}, contained {5}",
                    room.Name,
                    room.Category,
                    FormatNullable(room.Tier),
                    FormatNullable(room.CurrentValue),
                    room.FurnitureCount?.ToString(CultureInfo.InvariantCulture) ?? "?",
                    room.Contained ? "yes" : "no"));

                lines.Add(cap == null
                    ? "  caps: unknown until room tier is confirmed"
                    : string.Format(
                        CultureInfo.InvariantCulture,
                        "  caps: soft {0}, hard {1}, diminishing {2}",
                        HousingFurnitureFormatter.FormatBaseValue(cap.SoftCap),
                        HousingFurnitureFormatter.FormatBaseValue(cap.HardCap),
                        HousingFurnitureFormatter.FormatMultiplier(cap.DiminishingReturnPercent)));
            }

            foreach (var warning in snapshot.Warnings)
            {
                lines.Add("Note: " + warning);
            }

            return string.Join(Environment.NewLine, lines);
        }

        public string RenderPropertyValue(
            HousingPropertyValueSnapshot snapshot,
            IReadOnlyList<HousingFurnitureGroup> groups = null,
            HousingAvailabilitySnapshot availability = null,
            HousingPropertyAdvice advice = null)
        {
            var lines = new List<string>();
            if (advice == null && groups != null && groups.Count > 0)
            {
                advice = new HousingPropertyAdviceEngine().BuildAdvice(snapshot, groups, availability ?? new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>()), 2, 3);
            }

            if (snapshot.Rooms.Count > 0)
            {
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "Best useful additions ({0} residence rooms):",
                    snapshot.Rooms.Count));

                foreach (var room in snapshot.Rooms.Take(5))
                {
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "- {0}: {1} XP/day, tier {2}",
                        room.RoomName,
                        FormatNullable(room.Value),
                        FormatNullable(room.Tier)));
                    if (room.ExistingTypeCounts.Count > 0)
                    {
                        lines.Add("  mapped types: " + room.FormatKnownExistingTypes());
                    }
                }

                if (advice != null)
                {
                    AddNewRoomAdvice(lines, advice);
                    foreach (var roomAdvice in advice.Rooms)
                    {
                        lines.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} {1} XP/day:",
                            roomAdvice.Room.RoomName,
                            FormatNullable(roomAdvice.Room.Value)));
                        foreach (var addition in roomAdvice.Additions)
                        {
                            var firstItem = addition.Group.Items[0];
                            lines.Add(string.Format(
                                CultureInfo.InvariantCulture,
                                "- {0} in {1}: +{2} XP/day est. ({3}, {4}{5})",
                                firstItem.DisplayName,
                                roomAdvice.Room.RoomName,
                                HousingFurnitureFormatter.FormatBaseValue(addition.EstimatedGain),
                                addition.Category,
                                addition.CapNote,
                                FormatDuplicateNote(addition)));
                            lines.Add("  " + FormatAvailability(addition.Availability));
                        }
                    }
                }
            }
            else
            {
                lines.Add("No residence rooms found yet.");
                if (advice != null && advice.NewRooms.Count > 0)
                {
                    AddNewRoomAdvice(lines, advice);
                }
                else
                {
                    lines.Add("Advisor is attached to PropertyValue, but no available room setup was found yet.");
                }
            }

            if (snapshot.TotalValue != null)
            {
                lines.Add("Total read: " + HousingFurnitureFormatter.FormatBaseValue(snapshot.TotalValue.Value) + " XP/day");
            }

            if (snapshot.ResidentCount != null)
            {
                lines.Add("Residents: " + snapshot.ResidentCount.Value.ToString(CultureInfo.InvariantCulture));
            }

            lines.Add("XP values are est.; architecture/culture multipliers are already reflected by Eco totals, not reapplied to suggestions.");
            return string.Join(Environment.NewLine, lines);
        }

        public string RenderPropertyTooltip(
            HousingPropertyValueSnapshot snapshot,
            IReadOnlyList<HousingFurnitureGroup> groups,
            HousingAvailabilitySnapshot availability,
            HousingPropertyAdvice advice = null)
        {
            if (advice == null)
            {
                advice = new HousingPropertyAdviceEngine().BuildAdvice(
                    snapshot,
                    groups ?? new HousingFurnitureGroup[0],
                    availability ?? new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>()),
                    2,
                    2);
            }

            var lines = new List<string>();
            var additions = advice.NewRooms
                .SelectMany(room => room.Additions.Select(addition => new { Room = room.Room.Category, Addition = addition }))
                .Concat(advice.Rooms.SelectMany(room => room.Additions.Select(addition => new { Room = room.Room.RoomName, Addition = addition })))
                .OrderByDescending(entry => entry.Addition.EstimatedGain)
                .Take(3)
                .ToArray();

            if (additions.Length == 0)
            {
                return "No useful available housing upgrade found.";
            }

            lines.Add("Best housing moves:");
            foreach (var entry in additions)
            {
                var item = entry.Addition.Group.Items[0];
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "- {0}: {1} +{2} XP/day est.",
                    entry.Room,
                    item.DisplayName,
                    HousingFurnitureFormatter.FormatBaseValue(entry.Addition.EstimatedGain)));
                lines.Add("  " + FormatAvailabilityShort(entry.Addition.Availability));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static void AddNewRoomAdvice(ICollection<string> lines, HousingPropertyAdvice advice)
        {
            if (advice.NewRooms.Count == 0)
            {
                return;
            }

            lines.Add("New useful room setups:");
            foreach (var roomAdvice in advice.NewRooms)
            {
                lines.Add(roomAdvice.Room.Category + ":");
                foreach (var addition in roomAdvice.Additions)
                {
                    var firstItem = addition.Group.Items[0];
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "- {0}: +{1} XP/day est. ({2})",
                        firstItem.DisplayName,
                        HousingFurnitureFormatter.FormatBaseValue(addition.EstimatedGain),
                        addition.CapNote));
                    lines.Add("  " + FormatAvailability(addition.Availability));
                }
            }
        }

        public string RenderSuggestions(HousingSuggestionResult result)
        {
            if (result.Suggestions.Count == 0)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: no suggestions for category '{0}'.",
                    result.Category);
            }

            var lines = new List<string>
            {
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Eco Housing Advisor: best additions for {0}. Showing {1}/{2}, page {3}/{4}.",
                    result.Category,
                    result.Suggestions.Count,
                    result.TotalMatches,
                    result.Page,
                    result.PageCount),
            };

            var index = 1;
            foreach (var suggestion in result.Suggestions)
            {
                var firstItem = suggestion.Group.Items[0];
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}. {1}: +{2} XP/day est., useful in {3}, type {4}",
                    index++,
                    firstItem.DisplayName,
                    HousingFurnitureFormatter.FormatBaseValue(suggestion.EstimatedGain),
                    HousingRoomRules.FormatUsefulRooms(suggestion.Group.Category),
                    suggestion.Group.TypeForRoomLimit));
                lines.Add("   " + FormatAvailability(suggestion.Availability));
            }

            if (result.PageCount > 1)
            {
                lines.Add(result.Page >= result.PageCount
                    ? "End of suggestions."
                    : string.Format(CultureInfo.InvariantCulture, "Next: /housingadvisor suggest {0} {1}", result.Category, result.Page + 1));
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static void AddGroupLines(List<string> lines, IReadOnlyList<HousingFurnitureGroup> groups)
        {
            foreach (var categoryGroup in groups.GroupBy(group => group.Category).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
            {
                lines.Add(categoryGroup.Key);
                foreach (var group in categoryGroup)
                {
                    var names = string.Join(", ", group.Items.Select(item => item.DisplayName));
                    lines.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "- {0}: base {1}, type limit {2}, duplicate multiplier {3}",
                        names,
                        HousingFurnitureFormatter.FormatBaseValue(group.BaseValue),
                        group.TypeForRoomLimit,
                        HousingFurnitureFormatter.FormatMultiplier(group.DiminishingReturnMultiplier)));
                }
            }
        }

        private static string NextPageHint(HousingFurnitureQueryResult result)
        {
            var query = result.Query;
            if (query.Page >= result.PageCount)
            {
                return "End of results.";
            }

            var nextPage = Math.Min(query.Page + 1, result.PageCount);
            if (query.Mode == "category" || query.Mode == "search")
            {
                return string.Format(CultureInfo.InvariantCulture, "Next: /housingadvisor {0} {1} {2}", query.Mode, query.Text, nextPage);
            }

            return "Next: /housingadvisor list " + nextPage.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatAvailability(HousingItemAvailability availability)
        {
            if (availability.OwnedLocations.Count > 0)
            {
                var location = availability.OwnedLocations[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Have: {0} in {1}",
                    HousingFurnitureFormatter.FormatBaseValue(location.Quantity),
                    location.LocationName);
            }

            if (availability.StoreOffers.Count > 0)
            {
                var offer = availability.StoreOffers[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Buy: {0} {1} at {2} ({3} in stock, seller {4})",
                    HousingFurnitureFormatter.FormatBaseValue(offer.Price),
                    offer.Currency,
                    offer.StoreName,
                    HousingFurnitureFormatter.FormatBaseValue(offer.Quantity),
                    offer.SellerName);
            }

            if (availability.CraftHints.Count > 0)
            {
                var craft = availability.CraftHints[0];
                if (craft.CraftableByAnyone)
                {
                    return "Craft: no skill required";
                }

                var crafters = craft.Crafters.Count == 0 ? "no known crafter found" : string.Join(", ", craft.Crafters);
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Craft: {0} level {1}; crafters: {2}",
                    craft.SkillName,
                    craft.RequiredLevel,
                    crafters);
            }

            return "Availability: not found in accessible shops; craft recipe unknown.";
        }

        private static string FormatAvailabilityShort(HousingItemAvailability availability)
        {
            if (availability.OwnedLocations.Count > 0)
            {
                var location = availability.OwnedLocations[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Have: {0} in {1}",
                    HousingFurnitureFormatter.FormatBaseValue(location.Quantity),
                    location.LocationName);
            }

            if (availability.StoreOffers.Count > 0)
            {
                var offer = availability.StoreOffers[0];
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Buy: {0} {1} at {2}, stock {3}",
                    HousingFurnitureFormatter.FormatBaseValue(offer.Price),
                    offer.Currency,
                    offer.StoreName,
                    HousingFurnitureFormatter.FormatBaseValue(offer.Quantity));
            }

            if (availability.CraftHints.Count > 0)
            {
                var craft = availability.CraftHints[0];
                if (craft.CraftableByAnyone)
                {
                    return "Craft: no skill required";
                }

                var crafters = craft.Crafters.Count == 0 ? "no known crafter" : string.Join(", ", craft.Crafters);
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Craft: {0} {1}; {2}",
                    craft.SkillName,
                    craft.RequiredLevel,
                    crafters);
            }

            return "Availability unknown";
        }

        private static string FormatDuplicateNote(HousingRoomAdditionAdvice addition)
        {
            return addition.ExistingTypeCount <= 0
                ? string.Empty
                : string.Format(
                    CultureInfo.InvariantCulture,
                    ", duplicate type x{0}, factor {1}",
                    addition.ExistingTypeCount,
                    HousingFurnitureFormatter.FormatMultiplier(addition.DuplicateFactor));
        }

        private static string FormatNullable(double? value)
        {
            return value == null
                ? "?"
                : HousingFurnitureFormatter.FormatBaseValue(value.Value);
        }

        private static string CompactLocString(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ").Trim();
        }
    }
}

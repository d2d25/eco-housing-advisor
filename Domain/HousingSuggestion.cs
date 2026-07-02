using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingSuggestion
    {
        public HousingSuggestion(HousingFurnitureGroup group, HousingItemAvailability availability)
        {
            this.Group = group;
            this.Availability = availability;
        }

        public HousingFurnitureGroup Group { get; }

        public HousingItemAvailability Availability { get; }

        public double EstimatedGain => this.Group.BaseValue;
    }

    public sealed class HousingSuggestionResult
    {
        public HousingSuggestionResult(
            string category,
            int page,
            int pageCount,
            int totalMatches,
            IReadOnlyList<HousingSuggestion> suggestions)
        {
            this.Category = category;
            this.Page = page;
            this.PageCount = pageCount;
            this.TotalMatches = totalMatches;
            this.Suggestions = suggestions;
        }

        public string Category { get; }

        public int Page { get; }

        public int PageCount { get; }

        public int TotalMatches { get; }

        public IReadOnlyList<HousingSuggestion> Suggestions { get; }
    }

    public sealed class HousingSuggestionEngine
    {
        public HousingSuggestionResult SuggestByCategory(
            IReadOnlyList<HousingFurnitureGroup> groups,
            HousingAvailabilitySnapshot availability,
            string category,
            int page,
            int pageSize)
        {
            var normalizedPage = page < 1 ? 1 : page;
            var normalizedPageSize = pageSize < 1 ? 5 : pageSize;
            var matches = groups
                .Where(group => Contains(group.Category, category))
                .Select(group => SelectAvailableSuggestion(group, availability))
                .Where(suggestion => suggestion.Availability.IsAvailable)
                .OrderByDescending(suggestion => suggestion.Group.BaseValue)
                .ThenBy(suggestion => suggestion.Group.TypeForRoomLimit, StringComparer.OrdinalIgnoreCase)
                .ThenBy(suggestion => suggestion.Group.Items[0].DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var pageCount = Math.Max(1, (int)Math.Ceiling(matches.Length / (double)normalizedPageSize));
            var boundedPage = Math.Min(normalizedPage, pageCount);

            return new HousingSuggestionResult(
                string.IsNullOrWhiteSpace(category) ? "all" : category.Trim(),
                boundedPage,
                pageCount,
                matches.Length,
                matches
                    .Skip((boundedPage - 1) * normalizedPageSize)
                    .Take(normalizedPageSize)
                    .ToArray());
        }

        private static bool Contains(string value, string search)
        {
            return string.IsNullOrWhiteSpace(search)
                || (value != null && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static HousingSuggestion SelectAvailableSuggestion(
            HousingFurnitureGroup group,
            HousingAvailabilitySnapshot availability)
        {
            if (group.Items.Count <= 1)
            {
                return new HousingSuggestion(group, availability.ForItem(group.Items[0].ItemTypeName));
            }

            var orderedItems = group.Items
                .OrderByDescending(item => AvailabilityPriority(availability.ForItem(item.ItemTypeName)))
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var orderedGroup = new HousingFurnitureGroup(
                group.Category,
                group.TypeForRoomLimit,
                group.BaseValue,
                group.DiminishingReturnMultiplier,
                orderedItems);
            return new HousingSuggestion(orderedGroup, availability.ForItem(orderedItems[0].ItemTypeName));
        }

        private static int AvailabilityPriority(HousingItemAvailability availability)
        {
            if (availability.OwnedLocations.Count > 0)
            {
                return 3;
            }

            if (availability.StoreOffers.Count > 0)
            {
                return 2;
            }

            return availability.CraftHints.Any(craft => craft.CraftableByAnyone || craft.Crafters.Count > 0)
                ? 1
                : 0;
        }
    }
}

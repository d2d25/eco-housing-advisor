using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingFurnitureBrowser
    {
        public HousingFurnitureQueryResult Query(
            IReadOnlyList<HousingFurnitureGroup> groups,
            HousingFurnitureQuery query)
        {
            var filtered = Filter(groups, query).ToArray();
            var pageCount = Math.Max(1, (int)Math.Ceiling(filtered.Length / (double)query.PageSize));
            var page = Math.Min(query.Page, pageCount);
            var pageGroups = filtered
                .Skip((page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToArray();

            var normalizedQuery = new HousingFurnitureQuery(query.Mode, query.Text, page, query.PageSize);
            return new HousingFurnitureQueryResult(
                normalizedQuery,
                groups.Sum(group => group.Items.Count),
                groups.Count,
                pageGroups,
                filtered.Length,
                pageCount,
                BuildMessage(normalizedQuery));
        }

        private static IEnumerable<HousingFurnitureGroup> Filter(
            IEnumerable<HousingFurnitureGroup> groups,
            HousingFurnitureQuery query)
        {
            if (query.Mode == "category")
            {
                return string.IsNullOrWhiteSpace(query.Text)
                    ? groups
                    : groups.Where(group => Contains(group.Category, query.Text));
            }

            if (query.Mode == "search")
            {
                return string.IsNullOrWhiteSpace(query.Text)
                    ? groups
                    : groups.Where(group =>
                        Contains(group.Category, query.Text)
                        || Contains(group.TypeForRoomLimit, query.Text)
                        || group.Items.Any(item => Contains(item.DisplayName, query.Text) || Contains(item.ItemTypeName, query.Text)));
            }

            return groups;
        }

        private static string BuildMessage(HousingFurnitureQuery query)
        {
            if (query.Mode == "category" && !string.IsNullOrWhiteSpace(query.Text))
            {
                return "Category: " + query.Text;
            }

            if (query.Mode == "search" && !string.IsNullOrWhiteSpace(query.Text))
            {
                return "Search: " + query.Text;
            }

            return "Summary";
        }

        private static bool Contains(string value, string search)
        {
            return value != null && search != null && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

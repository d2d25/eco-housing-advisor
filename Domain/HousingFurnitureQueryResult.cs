using System.Collections.Generic;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingFurnitureQueryResult
    {
        public HousingFurnitureQueryResult(
            HousingFurnitureQuery query,
            int totalFurniture,
            int totalGroups,
            IReadOnlyList<HousingFurnitureGroup> groups,
            int filteredGroupCount,
            int pageCount,
            string message)
        {
            this.Query = query;
            this.TotalFurniture = totalFurniture;
            this.TotalGroups = totalGroups;
            this.Groups = groups;
            this.FilteredGroupCount = filteredGroupCount;
            this.PageCount = pageCount;
            this.Message = message;
        }

        public HousingFurnitureQuery Query { get; }

        public int TotalFurniture { get; }

        public int TotalGroups { get; }

        public IReadOnlyList<HousingFurnitureGroup> Groups { get; }

        public int FilteredGroupCount { get; }

        public int PageCount { get; }

        public string Message { get; }
    }
}

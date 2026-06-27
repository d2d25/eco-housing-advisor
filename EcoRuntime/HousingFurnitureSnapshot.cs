using System;
using System.Collections.Generic;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class HousingFurnitureSnapshot
    {
        public HousingFurnitureSnapshot(
            DateTimeOffset generatedAt,
            IReadOnlyList<HousingFurnitureItem> furniture,
            IReadOnlyList<HousingFurnitureGroup> groups)
        {
            this.GeneratedAt = generatedAt;
            this.Furniture = furniture;
            this.Groups = groups;
        }

        public DateTimeOffset GeneratedAt { get; }

        public IReadOnlyList<HousingFurnitureItem> Furniture { get; }

        public IReadOnlyList<HousingFurnitureGroup> Groups { get; }
    }
}

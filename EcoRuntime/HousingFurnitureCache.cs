using System;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class HousingFurnitureCache
    {
        private readonly object sync = new object();
        private readonly IEcoFurnitureReader reader;
        private readonly HousingFurnitureGrouper grouper;
        private HousingFurnitureSnapshot snapshot;

        public HousingFurnitureCache(IEcoFurnitureReader reader, HousingFurnitureGrouper grouper)
        {
            this.reader = reader;
            this.grouper = grouper;
        }

        public HousingFurnitureSnapshot Get(bool refresh)
        {
            lock (this.sync)
            {
                if (this.snapshot == null || refresh)
                {
                    var furniture = this.reader.ReadFurniture();
                    var groups = this.grouper.GroupFurniture(furniture);
                    this.snapshot = new HousingFurnitureSnapshot(DateTimeOffset.Now, furniture, groups);
                }

                return this.snapshot;
            }
        }
    }
}

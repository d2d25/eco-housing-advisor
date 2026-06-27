using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public static class HousingAdvisorRuntime
    {
        private static readonly HousingFurnitureCache Cache = new HousingFurnitureCache(
            new EcoFurnitureReader(),
            new HousingFurnitureGrouper());

        public static HousingFurnitureSnapshot GetSnapshot(bool refresh)
        {
            return Cache.Get(refresh);
        }
    }
}

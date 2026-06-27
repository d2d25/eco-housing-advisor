using EcoHousingAdvisor.Domain;
#if ECO_MODKIT
using Eco.Gameplay.Players;
#endif

namespace EcoHousingAdvisor.EcoRuntime
{
    public static class HousingAdvisorRuntime
    {
        private static readonly HousingFurnitureCache Cache = new HousingFurnitureCache(
            new EcoFurnitureReader(),
            new HousingFurnitureGrouper());
#if ECO_MODKIT
        private static readonly EcoAvailabilityReader AvailabilityReader = new EcoAvailabilityReader();
#endif

        public static HousingFurnitureSnapshot GetSnapshot(bool refresh)
        {
            return Cache.Get(refresh);
        }

#if ECO_MODKIT
        public static HousingAvailabilitySnapshot GetAvailability(User user, HousingFurnitureSnapshot snapshot)
        {
            return AvailabilityReader.ReadAvailability(user, snapshot.Furniture);
        }
#endif
    }
}

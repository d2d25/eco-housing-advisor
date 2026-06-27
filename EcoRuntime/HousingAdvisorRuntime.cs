using EcoHousingAdvisor.Domain;
using Eco.Gameplay.Players;

namespace EcoHousingAdvisor.EcoRuntime
{
    public static class HousingAdvisorRuntime
    {
        private static readonly HousingFurnitureCache Cache = new HousingFurnitureCache(
            new EcoFurnitureReader(),
            new HousingFurnitureGrouper());
        private static readonly EcoAvailabilityReader AvailabilityReader = new EcoAvailabilityReader();
        private static readonly EcoResidenceReader ResidenceReader = new EcoResidenceReader();

        public static HousingFurnitureSnapshot GetSnapshot(bool refresh)
        {
            return Cache.Get(refresh);
        }

        public static HousingAvailabilitySnapshot GetAvailability(User user, HousingFurnitureSnapshot snapshot)
        {
            return AvailabilityReader.ReadAvailability(user, snapshot.Furniture);
        }

        public static HousingResidenceSnapshot GetResidence(User user)
        {
            return ResidenceReader.ReadResidence(user);
        }
    }
}

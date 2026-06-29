using System;
using System.Collections.Generic;
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
        private static readonly EcoRoomDiagnosticsReader RoomDiagnosticsReader = new EcoRoomDiagnosticsReader();
        private static readonly Dictionary<string, TimedAvailability> AvailabilityCache = new Dictionary<string, TimedAvailability>(StringComparer.Ordinal);
        private static readonly Dictionary<int, TimedPropertyAdvice> PropertyAdviceCache = new Dictionary<int, TimedPropertyAdvice>();
        private static readonly TimeSpan AvailabilityTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan PropertyAdviceTtl = TimeSpan.FromSeconds(10);

        public static HousingFurnitureSnapshot GetSnapshot(bool refresh)
        {
            return Cache.Get(refresh);
        }

        public static HousingAvailabilitySnapshot GetAvailability(User user, HousingFurnitureSnapshot snapshot)
        {
            var key = (user?.Name ?? "unknown") + ":" + snapshot.GeneratedAt.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (AvailabilityCache.TryGetValue(key, out var cached) && DateTimeOffset.UtcNow - cached.CreatedAt < AvailabilityTtl)
            {
                return cached.Availability;
            }

            var availability = AvailabilityReader.ReadAvailability(user, snapshot.Furniture);
            AvailabilityCache[key] = new TimedAvailability(availability, DateTimeOffset.UtcNow);
            return availability;
        }

        public static HousingResidenceSnapshot GetResidence(User user)
        {
            return ResidenceReader.ReadResidence(user);
        }

        public static HousingRoomDiagnostics GetCurrentRoomDiagnostics(User user)
        {
            return RoomDiagnosticsReader.ReadCurrentRoom(user);
        }

        public static HousingPropertyAdvice GetPropertyAdvice(
            object propertyValue,
            HousingPropertyValueSnapshot property,
            HousingFurnitureSnapshot furniture,
            HousingAvailabilitySnapshot availability)
        {
            var key = propertyValue == null ? 0 : System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(propertyValue);
            if (key != 0 && PropertyAdviceCache.TryGetValue(key, out var cached) && DateTimeOffset.UtcNow - cached.CreatedAt < PropertyAdviceTtl)
            {
                return cached.Advice;
            }

            var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, furniture.Groups, availability, 2, 3);
            if (key != 0)
            {
                PropertyAdviceCache[key] = new TimedPropertyAdvice(advice, DateTimeOffset.UtcNow);
            }

            return advice;
        }

        private sealed class TimedAvailability
        {
            public TimedAvailability(HousingAvailabilitySnapshot availability, DateTimeOffset createdAt)
            {
                this.Availability = availability;
                this.CreatedAt = createdAt;
            }

            public HousingAvailabilitySnapshot Availability { get; }

            public DateTimeOffset CreatedAt { get; }
        }

        private sealed class TimedPropertyAdvice
        {
            public TimedPropertyAdvice(HousingPropertyAdvice advice, DateTimeOffset createdAt)
            {
                this.Advice = advice;
                this.CreatedAt = createdAt;
            }

            public HousingPropertyAdvice Advice { get; }

            public DateTimeOffset CreatedAt { get; }
        }
    }
}

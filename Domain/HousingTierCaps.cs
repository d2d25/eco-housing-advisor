using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingTierCap
    {
        public HousingTierCap(double tier, double softCap, double hardCap, double diminishingReturnPercent)
        {
            this.Tier = tier;
            this.SoftCap = softCap;
            this.HardCap = hardCap;
            this.DiminishingReturnPercent = diminishingReturnPercent;
        }

        public double Tier { get; }

        public double SoftCap { get; }

        public double HardCap { get; }

        public double DiminishingReturnPercent { get; }
    }

    public static class HousingTierCaps
    {
        private static readonly IReadOnlyList<HousingTierCap> Caps = new[]
        {
            new HousingTierCap(0, 2, 4, 0.65),
            new HousingTierCap(1, 5, 10, 0.65),
            new HousingTierCap(2, 10, 20, 0.65),
            new HousingTierCap(3, 15, 30, 0.65),
            new HousingTierCap(4, 20, 40, 0.65),
            new HousingTierCap(5, 25, 50, 0.65),
        };

        public static HousingTierCap ForTier(double? tier)
        {
            if (tier == null || double.IsNaN(tier.Value))
            {
                return null;
            }

            return Caps
                .Where(cap => cap.Tier <= tier.Value)
                .OrderByDescending(cap => cap.Tier)
                .FirstOrDefault()
                ?? Caps[0];
        }
    }
}

using System;
using System.Linq;
using Eco.Gameplay.Property;
using Eco.Gameplay.Items;
using Eco.Gameplay.Housing.PropertyValues;
using Eco.Gameplay.Players;
using Eco.Gameplay.Players.Food;
using Eco.Gameplay.Systems.NewTooltip;
using Eco.Gameplay.Systems.NewTooltip.TooltipLibraryFiles;
using Eco.Shared.Items;
using Eco.Shared.Localization;
using Eco.Shared.Logging;
using Eco.Shared.Utils;
using EcoHousingAdvisor.EcoRuntime;
using EcoHousingAdvisor.Presentation;

namespace Eco.Mods.TechTree
{
    [TooltipLibrary]
    public static class HousingAdvisorTooltipLibrary
    {
        private const string TestMarker = "### ECO HOUSING ADVISOR TEST ###";

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(Item))]
        public static LocString HousingAdvisorTooltip(this Item ecoItem, User user)
        {
            try
            {
                var snapshot = HousingAdvisorRuntime.GetSnapshot(false);
                var item = snapshot.Furniture.FirstOrDefault(entry => entry.ItemTypeName == ecoItem.GetType().Name);
                if (item == null)
                {
                    return LocString.Empty;
                }

                return new TooltipSection(
                    Localizer.DoStr("HA-ITEM HOUSING ITEM"),
                    Localizer.NotLocalized($"{TestMarker}{Environment.NewLine}{HousingFurnitureFormatter.FormatTooltip(item)}"));
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.Do($"[EcoHousingAdvisor] Failed to generate housing tooltip for {ecoItem.GetType().Name}."));
                Log.WriteException(exception);
                return LocString.Empty;
            }
        }

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(FoodItem))]
        public static LocString HousingAdvisorFoodProbeTooltip(this FoodItem foodItem, User user)
        {
            try
            {
                return Localizer.NotLocalized(
                    $"{TestMarker}{Environment.NewLine}HA-FOOD OK on {foodItem.GetType().Name}{Environment.NewLine}If you see this on food, OpenNutriView-style hooks work.");
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.DoStr("[EcoHousingAdvisor] Failed to generate FoodItem probe tooltip."));
                Log.WriteException(exception);
                return LocString.Empty;
            }
        }

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(Stomach))]
        public static LocString HousingAdvisorStomachProbeTooltip(this Stomach stomach)
        {
            try
            {
                var owner = stomach?.Owner?.Name ?? "unknown";
                return Localizer.NotLocalized(
                    $"{TestMarker}{Environment.NewLine}HA-STOMACH OK for {owner}{Environment.NewLine}This matches OpenNutriView's stomach hook.");
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.DoStr("[EcoHousingAdvisor] Failed to generate Stomach probe tooltip."));
                Log.WriteException(exception);
                return LocString.Empty;
            }
        }

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(PropertyValue))]
        public static LocString HousingAdvisorPropertyValueTooltip(this PropertyValue propertyValue, User user)
        {
            return BuildPropertyValueTooltip(propertyValue, user, "HA-PROPERTYVALUE");
        }

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(ResidencyPropertyValue))]
        public static LocString HousingAdvisorResidencyPropertyValueTooltip(this ResidencyPropertyValue propertyValue, User user)
        {
            return BuildPropertyValueTooltip(propertyValue, user, "HA-RESIDENCY");
        }

        [NewTooltip(CacheAs.Disabled, overrideType: typeof(Deed))]
        public static LocString HousingAdvisorDeedTooltip(this Deed deed, User user)
        {
            return deed?.PropertyValue is ResidencyPropertyValue propertyValue
                ? BuildPropertyValueTooltip(propertyValue, user, "HA-DEED")
                : LocString.Empty;
        }

        private static LocString BuildPropertyValueTooltip(PropertyValue propertyValue, User user, string marker)
        {
            try
            {
                var snapshot = new EcoPropertyValueReader().Read(propertyValue);
                var furniture = HousingAdvisorRuntime.GetSnapshot(false);
                var availability = HousingAdvisorRuntime.GetAvailability(user, furniture);
                var advice = HousingAdvisorRuntime.GetPropertyAdvice(propertyValue, snapshot, furniture, availability);
                return Localizer.NotLocalized(
                    $"{TestMarker}{Environment.NewLine}{marker} OK{Environment.NewLine}{new AdvisorTextRenderer().RenderPropertyValue(snapshot, furniture.Groups, availability, advice)}");
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.DoStr("[EcoHousingAdvisor] Failed to generate PropertyValue tooltip."));
                Log.WriteException(exception);
                return LocString.Empty;
            }
        }
    }
}

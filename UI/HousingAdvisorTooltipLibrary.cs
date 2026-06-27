#if ECO_MODKIT
using System;
using System.Linq;
using Eco.Gameplay.Items;
using Eco.Gameplay.Players;
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
                    Localizer.DoStr("Eco Housing Advisor"),
                    Localizer.NotLocalized($"{HousingFurnitureFormatter.FormatTooltip(item)}"));
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.Do($"[EcoHousingAdvisor] Failed to generate housing tooltip for {ecoItem.GetType().Name}."));
                Log.WriteException(exception);
                return LocString.Empty;
            }
        }
    }
}
#endif

using System.Globalization;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.Presentation
{
    public static class HousingFurnitureFormatter
    {
        public static string FormatMultiplier(double? multiplier)
        {
            return multiplier == null ? "unknown" : multiplier.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatBaseValue(double baseValue)
        {
            return baseValue.ToString("0.##", CultureInfo.InvariantCulture);
        }

        public static string FormatTooltip(HousingFurnitureItem item)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Category: {0}\nBase value: {1}\nType limit: {2}\nDuplicate multiplier: {3}",
                item.Category,
                FormatBaseValue(item.BaseValue),
                string.IsNullOrWhiteSpace(item.TypeForRoomLimit) ? "None" : item.TypeForRoomLimit,
                FormatMultiplier(item.DiminishingReturnMultiplier));
        }
    }
}

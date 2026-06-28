using System;
using Eco.Gameplay.Housing.PropertyValues;
using Eco.Gameplay.Property;
using Eco.Shared.Localization;
using Eco.Shared.Logging;
using EcoHousingAdvisor.Presentation;

namespace EcoHousingAdvisor.EcoRuntime
{
    public static class PropertyValuePanelInjector
    {
        private const string Marker = "Eco Housing Advisor";

        public static int ApplyAll()
        {
            var count = 0;
            foreach (var deed in PropertyManager.GetAllDeeds())
            {
                if (Apply(deed))
                {
                    count++;
                }
            }

            return count;
        }

        public static bool Apply(Deed deed)
        {
            try
            {
                var propertyValue = deed?.PropertyValue as ResidencyPropertyValue;
                if (propertyValue == null)
                {
                    return false;
                }

                var snapshot = new EcoPropertyValueReader().Read(propertyValue);
                var furniture = HousingAdvisorRuntime.GetSnapshot(false);
                var availability = HousingAdvisorRuntime.GetAvailability(null, furniture);
                var advice = HousingAdvisorRuntime.GetPropertyAdvice(propertyValue, snapshot, furniture, availability);
                var advisorText = new AdvisorTextRenderer().RenderPropertyValue(snapshot, furniture.Groups, availability, advice);
                if (string.IsNullOrWhiteSpace(advisorText))
                {
                    return false;
                }

                propertyValue.Summary = AppendAdvisorBlock(propertyValue.Summary.ToString(), advisorText);
                propertyValue.Description = AppendAdvisorBlock(propertyValue.Description.ToString(), advisorText);
                propertyValue.Appendix = AppendAdvisorBlock(propertyValue.Appendix.ToString(), advisorText);
                return true;
            }
            catch (Exception exception)
            {
                Log.WriteError(Localizer.DoStr("[EcoHousingAdvisor] Failed to inject property value panel advice."));
                Log.WriteException(exception);
                return false;
            }
        }

        private static string RemoveExistingAdvisorBlock(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var markerIndex = text.IndexOf(Marker, StringComparison.Ordinal);
            return markerIndex < 0 ? text : text.Substring(0, markerIndex);
        }

        private static LocString AppendAdvisorBlock(string currentText, string advisorText)
        {
            var cleaned = RemoveExistingAdvisorBlock(currentText).TrimEnd();
            return Localizer.NotLocalizedStr(cleaned + Environment.NewLine + Environment.NewLine + Marker + Environment.NewLine + advisorText);
        }
    }
}

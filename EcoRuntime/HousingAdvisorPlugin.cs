#if ECO_MODKIT
using Eco.Core.Plugins.Interfaces;
using Eco.Core.Utils;
using Eco.Gameplay.Property;
using Eco.Shared.Localization;

namespace Eco.Mods.TechTree
{
    public sealed class HousingAdvisorPlugin : IModKitPlugin, IInitializablePlugin
    {
        public string GetCategory()
        {
            return Localizer.DoStr("Mods");
        }

        public string GetStatus()
        {
            return string.Empty;
        }

        public override string ToString()
        {
            return Localizer.DoStr("Eco Housing Advisor");
        }

        public void Initialize(TimedTask timer)
        {
            Deed.PropertyValueChangedEvent.Add(deed => EcoHousingAdvisor.EcoRuntime.PropertyValuePanelInjector.Apply(deed));
            EcoHousingAdvisor.EcoRuntime.PropertyValuePanelInjector.ApplyAll();
        }
    }
}
#endif

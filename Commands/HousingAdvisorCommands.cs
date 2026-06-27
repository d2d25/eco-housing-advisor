#if ECO_MODKIT
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.EcoRuntime;
using EcoHousingAdvisor.Presentation;

namespace EcoHousingAdvisor.Commands
{
    [ChatCommandHandler]
    public static class HousingAdvisorCommands
    {
        private const int PageSize = 8;
        private static readonly HousingFurnitureCache Cache = new HousingFurnitureCache(
            new EcoFurnitureReader(),
            new HousingFurnitureGrouper());

        [ChatCommand("List housing furniture values discovered from the Eco runtime.")]
        public static void HousingAdvisor(User user, string action = null, string value = null, int page = 1)
        {
            var refresh = Is(action, "refresh");
            var snapshot = Cache.Get(refresh);
            var renderer = new AdvisorTextRenderer();
            var text = Is(action, "debug")
                ? renderer.RenderDebug(snapshot)
                : renderer.RenderFurnitureResult(new HousingFurnitureBrowser().Query(
                    snapshot.Groups,
                    ParseQuery(action, value, page)));
            ChatManager.SendMessage(user, ChannelManager.Obj.Get(SpecialChannel.General), text);
        }

        private static HousingFurnitureQuery ParseQuery(string action, string value, int page)
        {
            if (Is(action, "category"))
            {
                return new HousingFurnitureQuery("category", value, page, PageSize);
            }

            if (Is(action, "search"))
            {
                return new HousingFurnitureQuery("search", value, page, PageSize);
            }

            var summaryPage = 1;
            int.TryParse(action, out summaryPage);
            return new HousingFurnitureQuery("summary", null, summaryPage < 1 ? page : summaryPage, PageSize);
        }

        private static bool Is(string actual, string expected)
        {
            return actual != null && actual.Equals(expected, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
#endif

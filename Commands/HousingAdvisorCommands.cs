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

        [ChatCommand("List housing furniture values discovered from the Eco runtime.")]
        public static void HousingAdvisor(User user)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, 1, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "List housing furniture summary.", "list")]
        public static void List(User user, int page = 1)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, page < 1 ? 1 : page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "List one housing furniture category.", "category")]
        public static void Category(User user, string name, int page = 1)
        {
            SendQuery(user, new HousingFurnitureQuery("category", name, page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "Search housing furniture by name, category, or type limit.", "search")]
        public static void Search(User user, string text, int page = 1)
        {
            SendQuery(user, new HousingFurnitureQuery("search", text, page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor discovery debug information.", "hadebug")]
        public static void HaDebug(User user)
        {
            var snapshot = HousingAdvisorRuntime.GetSnapshot(false);
            Send(user, new AdvisorTextRenderer().RenderDebug(snapshot));
        }

        [ChatSubCommand("HousingAdvisor", "Refresh the cached housing furniture snapshot.", "harefresh")]
        public static void HaRefresh(User user)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, 1, PageSize), true);
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor UI status.", "uistatus")]
        public static void UiStatus(User user)
        {
            Send(user, "Eco Housing Advisor UI: furniture item tooltip probe is installed. Hover housing items to test.");
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor help.", "hahelp")]
        public static void HaHelp(User user)
        {
            Send(user, new AdvisorTextRenderer().RenderHelp());
        }

        private static void SendQuery(User user, HousingFurnitureQuery query, bool refresh)
        {
            var snapshot = HousingAdvisorRuntime.GetSnapshot(refresh);
            var text = new AdvisorTextRenderer().RenderFurnitureResult(new HousingFurnitureBrowser().Query(snapshot.Groups, query));
            Send(user, text);
        }

        private static void Send(User user, string text)
        {
            ChatManager.SendMessage(user, ChannelManager.Obj.Get(SpecialChannel.General), text);
        }
    }
}
#endif

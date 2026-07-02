using Eco.Gameplay.Players;
using Eco.Gameplay.Systems.Messaging.Chat;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Gameplay.Systems.Messaging.Chat.Commands;
using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.EcoRuntime;
using EcoHousingAdvisor.Presentation;
using System;
using System.Globalization;

namespace EcoHousingAdvisor.Commands
{
    [ChatCommandHandler]
    public static class HousingAdvisorCommands
    {
        private const int PageSize = 8;

        [ChatCommand("Eco Housing Advisor admin diagnostics.", ChatAuthorizationLevel.Admin)]
        public static void HousingAdvisor(User user)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, 1, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "List housing furniture summary.", "list", ChatAuthorizationLevel.Admin)]
        public static void List(User user, int page = 1)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, page < 1 ? 1 : page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "List one housing furniture category.", "category", ChatAuthorizationLevel.Admin)]
        public static void Category(User user, string name, int page = 1)
        {
            page = ExtractTrailingPage(ref name, page);
            SendQuery(user, new HousingFurnitureQuery("category", name, page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "Search housing furniture by name, category, or type limit.", "search", ChatAuthorizationLevel.Admin)]
        public static void Search(User user, string text, int page = 1)
        {
            page = ExtractTrailingPage(ref text, page);
            SendQuery(user, new HousingFurnitureQuery("search", text, page, PageSize), false);
        }

        [ChatSubCommand("HousingAdvisor", "Suggest housing additions to buy or craft for one category.", "suggest", ChatAuthorizationLevel.Admin)]
        public static void Suggest(User user, string category, int page = 1)
        {
            page = ExtractTrailingPage(ref category, page);
            var snapshot = HousingAdvisorRuntime.GetSnapshot(false);
            var availability = HousingAdvisorRuntime.GetAvailability(user, snapshot);
            var result = new HousingSuggestionEngine().SuggestByCategory(
                snapshot.Groups,
                availability,
                category,
                page,
                5);
            Send(user, new AdvisorTextRenderer().RenderSuggestions(result));
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor discovery debug information.", "hadebug", ChatAuthorizationLevel.Admin)]
        public static void HaDebug(User user)
        {
            var snapshot = HousingAdvisorRuntime.GetSnapshot(false);
            Send(user, new AdvisorTextRenderer().RenderDebug(snapshot));
        }

        [ChatSubCommand("HousingAdvisor", "Refresh the cached housing furniture snapshot.", "harefresh", ChatAuthorizationLevel.Admin)]
        public static void HaRefresh(User user)
        {
            SendQuery(user, new HousingFurnitureQuery("summary", null, 1, PageSize), true);
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor UI status.", "uistatus", ChatAuthorizationLevel.Admin)]
        public static void UiStatus(User user)
        {
            Send(user, "Eco Housing Advisor UI: housing item, deed, and residency property value tooltips are installed.");
        }

        [ChatSubCommand("HousingAdvisor", "Probe your residence rooms, tiers, and caps.", "haresidence", ChatAuthorizationLevel.Admin)]
        public static void Residence(User user)
        {
            Send(user, new AdvisorTextRenderer().RenderRooms(HousingAdvisorRuntime.GetActiveResidenceProperty(user)));
        }

        [ChatSubCommand("HousingAdvisor", "List rooms from your active residence.", "harooms", ChatAuthorizationLevel.Admin)]
        public static void HaRooms(User user)
        {
            Send(user, new AdvisorTextRenderer().RenderRooms(HousingAdvisorRuntime.GetActiveResidenceProperty(user)));
        }

        [ChatSubCommand("HousingAdvisor", "Show furniture details for one active residence room type.", "haroom", ChatAuthorizationLevel.Admin)]
        public static void HaRoom(User user, string roomType)
        {
            Send(user, new AdvisorTextRenderer().RenderRoomDetails(HousingAdvisorRuntime.GetActiveResidenceProperty(user), roomType));
        }

        [ChatSubCommand("HousingAdvisor", "Show item category and room cap diagnostics.", "haitem", ChatAuthorizationLevel.Admin)]
        public static void HaItem(User user, string text)
        {
            var furniture = HousingAdvisorRuntime.GetSnapshot(false);
            var property = HousingAdvisorRuntime.GetActiveResidenceProperty(user);
            Send(user, new AdvisorTextRenderer().RenderItemDiagnostics(property, furniture.Groups, text));
        }

        [ChatSubCommand("HousingAdvisor", "Audit advisor housing rules against Eco runtime/source rules.", "haarules", ChatAuthorizationLevel.Admin)]
        public static void HaRules(User user)
        {
            Send(user, new EcoHousingRuleAudit().Run());
        }

        [ChatSubCommand("HousingAdvisor", "Audit advisor housing rules against Eco runtime/source rules.", "harules", ChatAuthorizationLevel.Admin)]
        public static void HaRulesAlias(User user)
        {
            Send(user, new EcoHousingRuleAudit().Run());
        }

        [ChatSubCommand("HousingAdvisor", "Show starter whole-property room setup advice.", "hastarter", ChatAuthorizationLevel.Admin)]
        public static void HaStarter(User user)
        {
            var furniture = HousingAdvisorRuntime.GetSnapshot(false);
            var availability = HousingAdvisorRuntime.GetAvailability(user, furniture);
            var property = new HousingPropertyValueSnapshot(
                "StarterProperty",
                0,
                1,
                1,
                new HousingPropertyRoomValue[0],
                new string[0],
                DateTimeOffset.UtcNow);
            Send(user, new AdvisorTextRenderer().RenderPropertyValue(property, furniture.Groups, availability));
        }

        [ChatSubCommand("HousingAdvisor", "Show Eco Housing Advisor admin help.", "hahelp", ChatAuthorizationLevel.Admin)]
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

        private static int ExtractTrailingPage(ref string text, int page)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return page < 1 ? 1 : page;
            }

            var trimmed = text.Trim();
            var lastSpace = trimmed.LastIndexOf(' ');
            if (lastSpace <= 0)
            {
                text = trimmed;
                return page < 1 ? 1 : page;
            }

            var lastToken = trimmed.Substring(lastSpace + 1);
            if (!int.TryParse(lastToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPage))
            {
                text = trimmed;
                return page < 1 ? 1 : page;
            }

            text = trimmed.Substring(0, lastSpace).Trim();
            return parsedPage < 1 ? 1 : parsedPage;
        }
    }
}

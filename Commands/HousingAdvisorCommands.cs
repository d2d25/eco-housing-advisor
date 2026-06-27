#if ECO_MODKIT
using System;
using System.Collections.Generic;
using System.Reflection;
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

        [ChatSubCommand("HousingAdvisor", "Suggest housing additions to buy or craft for one category.", "suggest")]
        public static void Suggest(User user, string category, int page = 1)
        {
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

        [ChatSubCommand("HousingAdvisor", "Show your Eco user identifiers for server admin setup.", "whoami")]
        public static void WhoAmI(User user)
        {
            var lines = new List<string>
            {
                "Eco Housing Advisor whoami:",
                "Name: " + SafeValue(user, "Name"),
            };

            foreach (var memberName in new[]
            {
                "Id",
                "ID",
                "SlgId",
                "SLGId",
                "SteamId",
                "SteamID",
                "SteamID64",
                "AccountId",
                "AccountID",
                "StrangeId",
                "StrangeID",
                "StrangeCloudId",
                "StrangeCloudID",
                "PlayerId",
                "PlayerID",
            })
            {
                var value = SafeValue(user, memberName);
                if (!string.IsNullOrWhiteSpace(value) && value != "<missing>")
                {
                    lines.Add(memberName + ": " + value);
                }
            }

            lines.Add("Use SteamID64 or SLG ID in Configs/Users.eco Admins.");
            Send(user, string.Join(Environment.NewLine, lines));
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

        private static string SafeValue(object instance, string memberName)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var property = instance.GetType().GetProperty(memberName, Flags);
            if (property != null)
            {
                var value = property.GetValue(instance);
                return value == null ? "<null>" : value.ToString();
            }

            var field = instance.GetType().GetField(memberName, Flags);
            if (field != null)
            {
                var value = field.GetValue(instance);
                return value == null ? "<null>" : value.ToString();
            }

            return "<missing>";
        }
    }
}
#endif

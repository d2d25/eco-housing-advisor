using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Components.Storage;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Shared.Items;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.Presentation;

namespace Eco.Mods.TechTree
{
    public static class HousingAdvisorRichTooltipRenderer
    {
        public static LocString RenderPropertyTooltip(HousingPropertyAdvice advice)
        {
            var entries = advice.NewRooms
                .SelectMany(room => room.Additions.Select(addition => new TooltipEntry(room.Room.Category, addition)))
                .Concat(advice.Rooms.SelectMany(room => room.Additions.Select(addition => new TooltipEntry(room.Room.RoomName, addition))))
                .OrderByDescending(entry => entry.Addition.EstimatedGain)
                .Take(3)
                .ToArray();

            if (entries.Length == 0)
            {
                return Localizer.DoStr("No useful available housing upgrade found.").Color("#C0C0C0");
            }

            var sb = new LocStringBuilder();
            sb.AppendLine(new LocString(Text.ColorUnity(
                Color.Yellow.UInt,
                TextLoc.SizeLoc(0.8f, FormattableStringFactory.Create("Best useful housing upgrades available to you:")).Italic())));

            foreach (var entry in entries)
            {
                var item = entry.Addition.Group.Items[0];
                var itemLink = ItemLink(item);
                var gain = PositiveGain(entry.Addition.EstimatedGain);
                var room = Text.Info(entry.Room);
                var places = AvailabilityLinks(entry.Addition.Availability);

                sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                    "{0} for {1} will provide you {2} XP/day est. and can be found here: {3}",
                    itemLink,
                    room,
                    gain,
                    places)));
            }

            return sb.ToLocString();
        }

        private static LocString ItemLink(HousingFurnitureItem item)
        {
            var type = FindType(item.ItemTypeName);
            if (type != null && Item.Get(type) is Item ecoItem)
            {
                return ecoItem.UILink();
            }

            return Localizer.NotLocalizedStr(item.DisplayName).Color("#00A7FF");
        }

        private static LocString AvailabilityLinks(HousingItemAvailability availability)
        {
            var links = OwnedLocationLinks(availability).ToList();
            if (links.Count == 0)
            {
                links.AddRange(StoreLinks(availability));
            }

            if (links.Count == 0)
            {
                links.AddRange(CrafterLinks(availability));
            }

            return links.Count == 0
                ? Localizer.DoStr("unknown").Color("#C0C0C0")
                : JoinLinks(links);
        }

        private static IEnumerable<LocString> OwnedLocationLinks(HousingItemAvailability availability)
        {
            if (availability.OwnedLocations.Count == 0)
            {
                yield break;
            }

            var itemType = FindType(availability.ItemTypeName);
            foreach (var location in availability.OwnedLocations.Take(2))
            {
                var quantity = Text.Info("x" + HousingFurnitureFormatter.FormatBaseValue(location.Quantity));
                if (location.PlayerInventory)
                {
                    yield return Localizer.Do(FormattableStringFactory.Create("{0} ({1})", Localizer.DoStr("Your inventory"), quantity));
                    continue;
                }

                var storage = FindStorage(itemType, location);
                var locationLink = storage?.Parent != null
                    ? storage.Parent.UILink()
                    : Localizer.NotLocalizedStr(location.LocationName).Color("#00A7FF");

                yield return Localizer.Do(FormattableStringFactory.Create("{0} ({1})", locationLink, quantity));
            }
        }

        private static IEnumerable<LocString> StoreLinks(HousingItemAvailability availability)
        {
            if (availability.StoreOffers.Count == 0)
            {
                yield break;
            }

            var itemType = FindType(availability.ItemTypeName);
            foreach (var offer in availability.StoreOffers.Take(2))
            {
                var store = FindStore(itemType, offer);
                var storeLink = store?.Parent != null
                    ? store.Parent.UILink()
                    : Localizer.NotLocalizedStr(offer.StoreName).Color("#00A7FF");
                var price = Localizer.NotLocalizedStr(
                    HousingFurnitureFormatter.FormatBaseValue(offer.Price) + " " + offer.Currency);
                var stock = Text.Info("stock " + HousingFurnitureFormatter.FormatBaseValue(offer.Quantity));

                yield return Localizer.Do(FormattableStringFactory.Create("{0} ({1}, {2})", storeLink, price, stock));
            }
        }

        private static IEnumerable<LocString> CrafterLinks(HousingItemAvailability availability)
        {
            foreach (var craft in availability.CraftHints.Take(1))
            {
                if (craft.CraftableByAnyone)
                {
                    yield return Localizer.DoStr("craftable: no skill required").Color("#7CFF4F");
                    continue;
                }

                var users = craft.Crafters
                    .Take(3)
                    .Select(UserLink)
                    .ToList();
                if (users.Count == 0)
                {
                    yield break;
                }

                var skill = Text.Info(craft.SkillName + " " + craft.RequiredLevel.ToString(CultureInfo.InvariantCulture));
                yield return Localizer.Do(FormattableStringFactory.Create("{0}: {1}", skill, JoinLinks(users)));
            }
        }

        private static LocString JoinLinks(IReadOnlyList<LocString> links)
        {
            if (links.Count == 0)
            {
                return LocString.Empty;
            }

            var result = links[0];
            for (var i = 1; i < links.Count; i++)
            {
                result = Localizer.Do(FormattableStringFactory.Create("{0}, {1}", result, links[i]));
            }

            return result;
        }

        private static LocString UserLink(string userName)
        {
            var user = UserManager.Users.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, userName, StringComparison.OrdinalIgnoreCase));
            if (user != null)
            {
                return Localizer.NotLocalizedStr(user.MarkedUpName);
            }

            return Localizer.NotLocalizedStr(userName).Color("#00A7FF");
        }

        private static StoreComponent FindStore(Type itemType, HousingStoreOffer offer)
        {
            if (itemType == null)
            {
                return null;
            }

            return WorldObjectUtil.AllObjsWithComponent<StoreComponent>()
                .Where(store => store != null && store.Parent != null && store.Enabled && store.Currency != null)
                .Where(store => string.Equals(ReadName(store.Parent), offer.StoreName, StringComparison.Ordinal)
                    && string.Equals(ReadName(store.Currency), offer.Currency, StringComparison.Ordinal))
                .FirstOrDefault(store => store.StoreData.SellOffers.Any(sell =>
                    sell?.Stack?.Item != null
                    && sell.Stack.Item.GetType() == itemType
                    && sell.Stack.Quantity > 0
                    && Math.Abs(Convert.ToDouble(sell.Price, CultureInfo.InvariantCulture) - offer.Price) < 0.001));
        }

        private static StorageComponent FindStorage(Type itemType, HousingOwnedItemLocation location)
        {
            if (itemType == null || location.PlayerInventory)
            {
                return null;
            }

            return WorldObjectUtil.AllObjsWithComponent<StorageComponent>()
                .Where(storage => storage != null && storage.Parent != null)
                .Where(storage => string.Equals(ReadName(storage.Parent), location.LocationName, StringComparison.Ordinal))
                .FirstOrDefault(storage => storage.Inventory.Stacks.Any(stack =>
                    stack?.Item != null
                    && stack.Item.GetType() == itemType
                    && stack.Quantity > 0));
        }

        private static Type FindType(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type => string.Equals(type.Name, typeName, StringComparison.Ordinal));
        }

        private static IEnumerable<Type> SafeGetTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).Cast<Type>();
            }
        }

        private static LocString PositiveGain(double value)
        {
            return Localizer.DoStr("+" + HousingFurnitureFormatter.FormatBaseValue(value)).Style(Text.Styles.Positive);
        }

        private static string ReadName(object instance)
        {
            return ReadString(instance, "Name")
                ?? ReadString(instance, "DisplayName")
                ?? ReadString(instance, "MarkedUpName")
                ?? instance?.ToString()
                ?? string.Empty;
        }

        private static string ReadString(object instance, string memberName)
        {
            if (instance == null)
            {
                return null;
            }

            const System.Reflection.BindingFlags Flags =
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance;
            var type = instance.GetType();
            var property = type.GetProperty(memberName, Flags);
            if (property != null)
            {
                return property.GetValue(instance)?.ToString();
            }

            var field = type.GetField(memberName, Flags);
            return field?.GetValue(instance)?.ToString();
        }

        private sealed class TooltipEntry
        {
            public TooltipEntry(string room, HousingRoomAdditionAdvice addition)
            {
                this.Room = room;
                this.Addition = addition;
            }

            public string Room { get; }

            public HousingRoomAdditionAdvice Addition { get; }
        }
    }
}

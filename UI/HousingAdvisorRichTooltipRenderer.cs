using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Components.Storage;
using Eco.Gameplay.Housing.PropertyValues;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Systems;
using Eco.Gameplay.Systems.TextLinks;
using Eco.Shared.Items;
using Eco.Shared.Localization;
using Eco.Shared.Utils;
using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.EcoRuntime;
using EcoHousingAdvisor.Presentation;

namespace Eco.Mods.TechTree
{
    public static class HousingAdvisorRichTooltipRenderer
    {
        public static LocString RenderItemTooltip(HousingFurnitureItem item)
        {
            var sb = new LocStringBuilder();
            sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                "Category: {0}",
                Link(HousingLinkTarget.RoomCategory(item.Category), item.Category))));
            sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                "Base value: {0}",
                PositiveGain(item.BaseValue))));
            sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                "Type limit: {0}",
                Localizer.NotLocalizedStr(item.TypeForRoomLimit ?? "none").Color("#00A7FF"))));
            sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                "Duplicate multiplier: {0}",
                Text.Info(HousingFurnitureFormatter.FormatMultiplier(item.DiminishingReturnMultiplier)))));
            return sb.ToLocString();
        }

        public static LocString RenderPropertyTooltip(HousingPropertyAdvice advice)
        {
            var entries = advice.NewRooms
                .SelectMany(room => room.Additions.Select(addition => new TooltipEntry(room.Room.RoomName, room.Room.Category, room.Room.RoomCategoryLink, true, addition)))
                .Concat(advice.Rooms.SelectMany(room => room.Additions.Select(addition => new TooltipEntry(room.Room.RoomName, room.Room.Category, room.Room.RoomCategoryLink, false, addition))))
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
                var room = PlacementLink(entry);
                var places = AvailabilityLinks(entry.Addition.Availability);

                sb.AppendLine(Localizer.Do(FormattableStringFactory.Create(
                    "{0} for {1} will provide you {2} XP/day and can be found here: {3}",
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
                    yield return Localizer.Do(FormattableStringFactory.Create("{0} ({1})", Link(location.LocationLink, "Your inventory"), quantity));
                    continue;
                }

                var storage = FindStorage(itemType, location);
                var locationLink = storage?.Parent != null
                    ? storage.Parent.UILink()
                    : Link(location.LocationLink, location.LocationName);

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
                    : Link(offer.StoreLink, offer.StoreName);
                var currency = Link(offer.CurrencyLink, offer.Currency);
                var price = Localizer.Do(FormattableStringFactory.Create(
                    "{0} {1}",
                    Localizer.NotLocalizedStr(HousingFurnitureFormatter.FormatBaseValue(offer.Price)),
                    currency));
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

                var users = craft.CrafterLinks.Count > 0
                    ? craft.CrafterLinks.Take(3).Select(link => Link(link, link.DisplayName)).ToList()
                    : craft.Crafters.Take(3).Select(UserLink).ToList();
                if (users.Count == 0)
                {
                    yield break;
                }

                var skill = Localizer.Do(FormattableStringFactory.Create(
                    "{0} {1}",
                    Link(craft.SkillLink, craft.SkillName),
                    Localizer.NotLocalizedStr(craft.RequiredLevel.ToString(CultureInfo.InvariantCulture))));
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

        private static LocString PlacementLink(TooltipEntry entry)
        {
            var category = entry.IsNewRoom
                ? RoomCategoryLink(entry.Category ?? entry.Room)
                : Link(entry.RoomLink, entry.Room ?? entry.Category);
            if (entry.IsNewRoom)
            {
                return Localizer.Do(FormattableStringFactory.Create("new {0}", category));
            }

            if (entry.RoomLink?.Kind == HousingLinkTargetKind.RoomValue)
            {
                return category;
            }

            return category;
        }

        private static LocString Link(HousingLinkTarget target, string fallback)
        {
            if (target == null)
            {
                return Localizer.NotLocalizedStr(fallback ?? "unknown").Color("#00A7FF");
            }

            switch (target.Kind)
            {
                case HousingLinkTargetKind.ItemType:
                    return ItemTypeLink(target.TypeName, target.DisplayName ?? fallback);
                case HousingLinkTargetKind.RoomValue:
                    return RegisteredEcoLinkOrFallback(target, fallback);
                case HousingLinkTargetKind.RoomCategory:
                    return RoomCategoryLink(target.TypeName ?? target.DisplayName ?? fallback);
                case HousingLinkTargetKind.Store:
                case HousingLinkTargetKind.Storage:
                case HousingLinkTargetKind.WorldObject:
                    return WorldObjectLinkOrFallback(target, fallback);
                case HousingLinkTargetKind.User:
                    return UserLink(target.DisplayName ?? fallback);
                case HousingLinkTargetKind.Inventory:
                    return Localizer.DoStr(target.DisplayName ?? fallback ?? "Your inventory").Color("#00A7FF");
                case HousingLinkTargetKind.Currency:
                case HousingLinkTargetKind.Skill:
                case HousingLinkTargetKind.Recipe:
                    return Localizer.NotLocalizedStr(target.DisplayName ?? fallback).Color("#00A7FF");
                default:
                    return Localizer.NotLocalizedStr(target.DisplayName ?? fallback ?? "unknown").Color("#00A7FF");
            }
        }

        private static LocString ItemTypeLink(string typeName, string fallback)
        {
            var type = FindType(typeName);
            if (type != null && Item.Get(type) is Item ecoItem)
            {
                return ecoItem.UILink();
            }

            return Localizer.NotLocalizedStr(fallback ?? typeName ?? "unknown").Color("#00A7FF");
        }

        private static LocString RoomCategoryLink(string categoryName)
        {
            try
            {
                var category = HousingConfig.GetRoomCategory(categoryName);
                if (category != null)
                {
                    return category.UILink();
                }
            }
            catch
            {
            }

            return Localizer.NotLocalizedStr(categoryName ?? "Room").Color("#00A7FF");
        }

        private static LocString RegisteredEcoLinkOrFallback(HousingLinkTarget target, string fallback)
        {
            if (EcoLinkTargetRegistry.TryGet(target, out var instance)
                && TryBuildEcoUiLink(instance, out var link))
            {
                return link;
            }

            return RoomCategoryLink(target.DisplayName ?? target.TypeName ?? fallback);
        }

        private static bool TryBuildEcoUiLink(object instance, out LocString link)
        {
            link = LocString.Empty;
            if (instance == null)
            {
                return false;
            }

            var instanceType = instance.GetType();
            foreach (var method in AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .SelectMany(type => type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
                .Where(method => method.Name == "UILink"
                    && method.IsDefined(typeof(ExtensionAttribute), false)
                    && method.GetParameters().Length >= 1
                    && method.GetParameters()[0].ParameterType.IsAssignableFrom(instanceType)))
            {
                try
                {
                    var parameters = method.GetParameters();
                    var args = new object[parameters.Length];
                    args[0] = instance;
                    for (var i = 1; i < args.Length; i++)
                    {
                        args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                    }

                    var value = method.Invoke(null, args);
                    if (value is LocString locString)
                    {
                        link = locString;
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        private static LocString WorldObjectLinkOrFallback(HousingLinkTarget target, string fallback)
        {
            var worldObject = FindWorldObject(target);
            return worldObject != null
                ? worldObject.UILink()
                : Localizer.NotLocalizedStr(target.DisplayName ?? fallback ?? "unknown").Color("#00A7FF");
        }

        private static StoreComponent FindStore(Type itemType, HousingStoreOffer offer)
        {
            if (itemType == null)
            {
                return null;
            }

            return WorldObjectUtil.AllObjsWithComponent<StoreComponent>()
                .Where(store => store != null && store.Parent != null && store.Enabled && store.Currency != null)
                .Where(store => MatchesTarget(store.Parent, offer.StoreLink, offer.StoreName)
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
                .Where(storage => MatchesTarget(storage.Parent, location.LocationLink, location.LocationName))
                .FirstOrDefault(storage => storage.Inventory.Stacks.Any(stack =>
                    stack?.Item != null
                    && stack.Item.GetType() == itemType
                    && stack.Quantity > 0));
        }

        private static WorldObject FindWorldObject(HousingLinkTarget target)
        {
            if (target == null)
            {
                return null;
            }

            var storeParent = WorldObjectUtil.AllObjsWithComponent<StoreComponent>()
                .Where(store => store?.Parent != null)
                .Select(store => store.Parent)
                .FirstOrDefault(parent => MatchesTarget(parent, target, target.DisplayName));
            if (storeParent != null)
            {
                return storeParent;
            }

            return WorldObjectUtil.AllObjsWithComponent<StorageComponent>()
                .Where(storage => storage?.Parent != null)
                .Select(storage => storage.Parent)
                .FirstOrDefault(parent => MatchesTarget(parent, target, target.DisplayName));
        }

        private static bool MatchesTarget(object instance, HousingLinkTarget target, string fallbackName)
        {
            if (instance == null)
            {
                return false;
            }

            if (target?.RuntimeId != null && RuntimeHelpers.GetHashCode(instance) == target.RuntimeId.Value)
            {
                return true;
            }

            return string.Equals(ReadName(instance), target?.DisplayName ?? fallbackName, StringComparison.Ordinal);
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
            public TooltipEntry(string room, string category, HousingLinkTarget roomLink, bool isNewRoom, HousingRoomAdditionAdvice addition)
            {
                this.Room = room;
                this.Category = category;
                this.RoomLink = roomLink;
                this.IsNewRoom = isNewRoom;
                this.Addition = addition;
            }

            public string Room { get; }

            public string Category { get; }

            public HousingLinkTarget RoomLink { get; }

            public bool IsNewRoom { get; }

            public HousingRoomAdditionAdvice Addition { get; }
        }
    }
}

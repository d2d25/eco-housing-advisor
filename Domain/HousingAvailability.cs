using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingAvailabilitySnapshot
    {
        public HousingAvailabilitySnapshot(IReadOnlyDictionary<string, HousingItemAvailability> items)
        {
            this.Items = items;
        }

        public IReadOnlyDictionary<string, HousingItemAvailability> Items { get; }

        public HousingItemAvailability ForItem(string itemTypeName)
        {
            return itemTypeName != null && this.Items.TryGetValue(itemTypeName, out var availability)
                ? availability
                : HousingItemAvailability.Empty(itemTypeName);
        }
    }

    public sealed class HousingItemAvailability
    {
        public HousingItemAvailability(
            string itemTypeName,
            IReadOnlyList<HousingStoreOffer> storeOffers,
            IReadOnlyList<HousingCraftHint> craftHints)
            : this(itemTypeName, new HousingOwnedItemLocation[0], storeOffers, craftHints)
        {
        }

        public HousingItemAvailability(
            string itemTypeName,
            IReadOnlyList<HousingOwnedItemLocation> ownedLocations,
            IReadOnlyList<HousingStoreOffer> storeOffers,
            IReadOnlyList<HousingCraftHint> craftHints)
        {
            this.ItemTypeName = itemTypeName;
            this.OwnedLocations = ownedLocations;
            this.StoreOffers = storeOffers;
            this.CraftHints = craftHints;
        }

        public string ItemTypeName { get; }

        public IReadOnlyList<HousingOwnedItemLocation> OwnedLocations { get; }

        public IReadOnlyList<HousingStoreOffer> StoreOffers { get; }

        public IReadOnlyList<HousingCraftHint> CraftHints { get; }

        public bool IsAvailable => this.OwnedLocations.Count > 0
            || this.StoreOffers.Count > 0
            || this.CraftHints.Any(craft => craft.CraftableByAnyone || craft.Crafters.Count > 0);

        public static HousingItemAvailability Empty(string itemTypeName)
        {
            return new HousingItemAvailability(itemTypeName, new HousingStoreOffer[0], new HousingCraftHint[0]);
        }
    }

    public sealed class HousingOwnedItemLocation
    {
        public HousingOwnedItemLocation(string locationName, bool playerInventory, double quantity)
            : this(locationName, playerInventory, quantity, playerInventory ? HousingLinkTarget.Inventory() : null)
        {
        }

        public HousingOwnedItemLocation(string locationName, bool playerInventory, double quantity, HousingLinkTarget locationLink)
        {
            this.LocationName = locationName;
            this.PlayerInventory = playerInventory;
            this.Quantity = quantity;
            this.LocationLink = locationLink;
        }

        public string LocationName { get; }

        public bool PlayerInventory { get; }

        public double Quantity { get; }

        public HousingLinkTarget LocationLink { get; }
    }

    public sealed class HousingStoreOffer
    {
        public HousingStoreOffer(string storeName, string sellerName, double price, string currency, double quantity)
            : this(storeName, sellerName, price, currency, quantity, null, null, null)
        {
        }

        public HousingStoreOffer(
            string storeName,
            string sellerName,
            double price,
            string currency,
            double quantity,
            HousingLinkTarget storeLink,
            HousingLinkTarget sellerLink,
            HousingLinkTarget currencyLink)
        {
            this.StoreName = storeName;
            this.SellerName = sellerName;
            this.Price = price;
            this.Currency = currency;
            this.Quantity = quantity;
            this.StoreLink = storeLink;
            this.SellerLink = sellerLink;
            this.CurrencyLink = currencyLink;
        }

        public string StoreName { get; }

        public string SellerName { get; }

        public double Price { get; }

        public string Currency { get; }

        public double Quantity { get; }

        public HousingLinkTarget StoreLink { get; }

        public HousingLinkTarget SellerLink { get; }

        public HousingLinkTarget CurrencyLink { get; }
    }

    public sealed class HousingCraftHint
    {
        public HousingCraftHint(string skillName, int requiredLevel, IReadOnlyList<string> crafters)
            : this(skillName, requiredLevel, crafters, false)
        {
        }

        public HousingCraftHint(string skillName, int requiredLevel, IReadOnlyList<string> crafters, bool craftableByAnyone)
            : this(skillName, requiredLevel, crafters, craftableByAnyone, null, null, null)
        {
        }

        public HousingCraftHint(
            string skillName,
            int requiredLevel,
            IReadOnlyList<string> crafters,
            bool craftableByAnyone,
            HousingLinkTarget skillLink,
            HousingLinkTarget recipeLink,
            IReadOnlyList<HousingLinkTarget> crafterLinks)
        {
            this.SkillName = skillName;
            this.RequiredLevel = requiredLevel;
            this.Crafters = crafters;
            this.CraftableByAnyone = craftableByAnyone;
            this.SkillLink = skillLink;
            this.RecipeLink = recipeLink;
            this.CrafterLinks = crafterLinks ?? new HousingLinkTarget[0];
        }

        public string SkillName { get; }

        public int RequiredLevel { get; }

        public IReadOnlyList<string> Crafters { get; }

        public bool CraftableByAnyone { get; }

        public HousingLinkTarget SkillLink { get; }

        public HousingLinkTarget RecipeLink { get; }

        public IReadOnlyList<HousingLinkTarget> CrafterLinks { get; }
    }
}

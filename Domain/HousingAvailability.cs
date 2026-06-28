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
        {
            this.LocationName = locationName;
            this.PlayerInventory = playerInventory;
            this.Quantity = quantity;
        }

        public string LocationName { get; }

        public bool PlayerInventory { get; }

        public double Quantity { get; }
    }

    public sealed class HousingStoreOffer
    {
        public HousingStoreOffer(string storeName, string sellerName, double price, string currency, double quantity)
        {
            this.StoreName = storeName;
            this.SellerName = sellerName;
            this.Price = price;
            this.Currency = currency;
            this.Quantity = quantity;
        }

        public string StoreName { get; }

        public string SellerName { get; }

        public double Price { get; }

        public string Currency { get; }

        public double Quantity { get; }
    }

    public sealed class HousingCraftHint
    {
        public HousingCraftHint(string skillName, int requiredLevel, IReadOnlyList<string> crafters)
            : this(skillName, requiredLevel, crafters, false)
        {
        }

        public HousingCraftHint(string skillName, int requiredLevel, IReadOnlyList<string> crafters, bool craftableByAnyone)
        {
            this.SkillName = skillName;
            this.RequiredLevel = requiredLevel;
            this.Crafters = crafters;
            this.CraftableByAnyone = craftableByAnyone;
        }

        public string SkillName { get; }

        public int RequiredLevel { get; }

        public IReadOnlyList<string> Crafters { get; }

        public bool CraftableByAnyone { get; }
    }
}

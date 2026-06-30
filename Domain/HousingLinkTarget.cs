namespace EcoHousingAdvisor.Domain
{
    public enum HousingLinkTargetKind
    {
        None,
        ItemType,
        WorldObject,
        RoomValue,
        RoomCategory,
        User,
        Store,
        Storage,
        Currency,
        Skill,
        Recipe,
        Inventory,
    }

    public sealed class HousingLinkTarget
    {
        public HousingLinkTarget(
            HousingLinkTargetKind kind,
            string displayName,
            string typeName = null,
            int? runtimeId = null,
            bool isPlayerInventory = false)
        {
            this.Kind = kind;
            this.DisplayName = displayName;
            this.TypeName = typeName;
            this.RuntimeId = runtimeId;
            this.IsPlayerInventory = isPlayerInventory;
        }

        public HousingLinkTargetKind Kind { get; }

        public string DisplayName { get; }

        public string TypeName { get; }

        public int? RuntimeId { get; }

        public bool IsPlayerInventory { get; }

        public static HousingLinkTarget ItemType(string typeName, string displayName)
        {
            return new HousingLinkTarget(HousingLinkTargetKind.ItemType, displayName, typeName);
        }

        public static HousingLinkTarget RoomCategory(string category)
        {
            return new HousingLinkTarget(HousingLinkTargetKind.RoomCategory, category, category);
        }

        public static HousingLinkTarget Inventory()
        {
            return new HousingLinkTarget(HousingLinkTargetKind.Inventory, "Your inventory", null, null, true);
        }
    }
}

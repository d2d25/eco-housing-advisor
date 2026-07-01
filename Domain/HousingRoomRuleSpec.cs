using System.Collections.Generic;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingRoomRuleSpec
    {
        public HousingRoomRuleSpec(
            string category,
            bool canBeRoomCategory,
            bool supportForAnyRoom,
            bool negatesValue,
            double maxSupportPercentOfPrimary,
            double capToPercentOfRestOfProperty,
            IReadOnlyList<string> supportingRoomCategoryNames)
        {
            this.Category = category;
            this.CanBeRoomCategory = canBeRoomCategory;
            this.SupportForAnyRoom = supportForAnyRoom;
            this.NegatesValue = negatesValue;
            this.MaxSupportPercentOfPrimary = maxSupportPercentOfPrimary;
            this.CapToPercentOfRestOfProperty = capToPercentOfRestOfProperty;
            this.SupportingRoomCategoryNames = supportingRoomCategoryNames ?? new string[0];
        }

        public string Category { get; }

        public bool CanBeRoomCategory { get; }

        public bool SupportForAnyRoom { get; }

        public bool NegatesValue { get; }

        public double MaxSupportPercentOfPrimary { get; }

        public double CapToPercentOfRestOfProperty { get; }

        public IReadOnlyList<string> SupportingRoomCategoryNames { get; }
    }
}

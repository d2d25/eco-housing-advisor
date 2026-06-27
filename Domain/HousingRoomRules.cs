using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoHousingAdvisor.Domain
{
    public sealed class HousingCategoryPlacement
    {
        public HousingCategoryPlacement(
            string category,
            bool canDefineRoom,
            bool supportForAnyRoom,
            IReadOnlyList<string> usefulRooms,
            string note)
        {
            this.Category = category;
            this.CanDefineRoom = canDefineRoom;
            this.SupportForAnyRoom = supportForAnyRoom;
            this.UsefulRooms = usefulRooms;
            this.Note = note;
        }

        public string Category { get; }

        public bool CanDefineRoom { get; }

        public bool SupportForAnyRoom { get; }

        public IReadOnlyList<string> UsefulRooms { get; }

        public string Note { get; }
    }

    public static class HousingRoomRules
    {
        private static readonly string[] ResidencePrimaryRooms =
        {
            "Living Room",
            "Bedroom",
            "Kitchen",
            "Bathroom",
            "Outdoor",
        };

        private static readonly Dictionary<string, HousingCategoryPlacement> Rules =
            new Dictionary<string, HousingCategoryPlacement>(StringComparer.OrdinalIgnoreCase)
            {
                ["Living Room"] = Primary("Living Room", "defines a residence living room; also supports bedrooms"),
                ["Bedroom"] = Primary("Bedroom", "defines a residence bedroom"),
                ["Kitchen"] = Primary("Kitchen", "defines a residence kitchen"),
                ["Bathroom"] = Primary("Bathroom", "defines a bathroom, capped against the rest of the property"),
                ["Outdoor"] = Primary("Outdoor", "outdoor room value; Eco does not auto-choose this category"),
                ["Cultural"] = new HousingCategoryPlacement(
                    "Cultural",
                    true,
                    false,
                    new[] { "Living Room", "Outdoor", "Cultural property" },
                    "supports living/outdoor rooms and defines cultural-property value"),
                ["Seating"] = new HousingCategoryPlacement(
                    "Seating",
                    false,
                    false,
                    new[] { "Living Room", "Bedroom", "Kitchen", "Bathroom", "Outdoor", "Cultural" },
                    "support category allowed by specific room types"),
                ["Decoration"] = new HousingCategoryPlacement(
                    "Decoration",
                    false,
                    true,
                    ResidencePrimaryRooms,
                    "support category for any room type"),
                ["Lighting"] = new HousingCategoryPlacement(
                    "Lighting",
                    false,
                    true,
                    ResidencePrimaryRooms,
                    "support category for any room type"),
                ["Industrial"] = new HousingCategoryPlacement(
                    "Industrial",
                    true,
                    false,
                    Array.Empty<string>(),
                    "negates housing value; avoid on residence property"),
            };

        public static HousingCategoryPlacement ForCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return Unknown(category);
            }

            return Rules.TryGetValue(category.Trim(), out var rule)
                ? rule
                : Unknown(category.Trim());
        }

        public static string FormatUsefulRooms(string category)
        {
            var rule = ForCategory(category);
            if (rule.UsefulRooms.Count == 0)
            {
                return rule.Note;
            }

            return string.Join(", ", rule.UsefulRooms);
        }

        private static HousingCategoryPlacement Primary(string category, string note)
        {
            return new HousingCategoryPlacement(category, true, false, category.SingleItemAsArray(), note);
        }

        private static HousingCategoryPlacement Unknown(string category)
        {
            var name = string.IsNullOrWhiteSpace(category) ? "Unknown" : category;
            return new HousingCategoryPlacement(name, true, false, new[] { name }, "unknown category; using the category itself");
        }

        private static T[] SingleItemAsArray<T>(this T value)
        {
            return new[] { value };
        }
    }
}

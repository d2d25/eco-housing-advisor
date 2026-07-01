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
        private static readonly string[] NonIndustrialRooms =
        {
            "Living Room",
            "Bedroom",
            "Kitchen",
            "Bathroom",
            "Outdoor",
            "Cultural",
        };

        private static readonly Dictionary<string, HousingCategoryPlacement> Rules =
            new Dictionary<string, HousingCategoryPlacement>(StringComparer.OrdinalIgnoreCase)
            {
                ["Living Room"] = new HousingCategoryPlacement(
                    "Living Room",
                    true,
                    false,
                    new[] { "Living Room", "Bedroom" },
                    "defines a living room and is accepted as bedroom support"),
                ["Bedroom"] = Primary("Bedroom", "defines a residence bedroom"),
                ["Kitchen"] = Primary("Kitchen", "defines a residence kitchen"),
                ["Bathroom"] = Primary("Bathroom", "defines a bathroom, capped against the rest of the property"),
                ["Outdoor"] = Primary("Outdoor", "outdoor room value; Eco does not auto-choose this category"),
                ["Cultural"] = new HousingCategoryPlacement(
                    "Cultural",
                    true,
                    false,
                    new[] { "Cultural", "Living Room", "Outdoor" },
                    "defines cultural value and is accepted as living/outdoor support"),
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
                    NonIndustrialRooms,
                    "support category for any room type"),
                ["Lighting"] = new HousingCategoryPlacement(
                    "Lighting",
                    false,
                    true,
                    NonIndustrialRooms,
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

        public static double SupportCapPercent(string supportCategory, string primaryRoomCategory)
        {
            supportCategory = NormalizeRoomName(supportCategory);
            primaryRoomCategory = NormalizeRoomName(primaryRoomCategory);

            if (string.Equals(supportCategory, "Seating", StringComparison.OrdinalIgnoreCase))
            {
                return 0.3;
            }

            if (string.Equals(supportCategory, "Decoration", StringComparison.OrdinalIgnoreCase)
                || string.Equals(supportCategory, "Lighting", StringComparison.OrdinalIgnoreCase))
            {
                return 0.5;
            }

            if (string.Equals(supportCategory, "Cultural", StringComparison.OrdinalIgnoreCase)
                && string.Equals(primaryRoomCategory, "Outdoor", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (string.Equals(supportCategory, "Cultural", StringComparison.OrdinalIgnoreCase))
            {
                return 0.2;
            }

            if (string.Equals(supportCategory, "Living Room", StringComparison.OrdinalIgnoreCase)
                && string.Equals(primaryRoomCategory, "Bedroom", StringComparison.OrdinalIgnoreCase))
            {
                return 0.25;
            }

            return 1;
        }

        public static double PropertyCategoryCapPercent(string roomCategory)
        {
            if (string.Equals(roomCategory, "Bathroom", StringComparison.OrdinalIgnoreCase))
            {
                return 0.33;
            }

            if (string.Equals(roomCategory, "Outdoor", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 0;
        }

        public static IReadOnlyList<string> CategoriesUsefulInRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                return Array.Empty<string>();
            }

            var normalized = NormalizeRoomName(roomName);
            return Rules.Values
                .Where(rule => !string.Equals(rule.Category, "Industrial", StringComparison.OrdinalIgnoreCase))
                .Where(rule => rule.UsefulRooms.Any(room => string.Equals(room, normalized, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(rule => rule.CanDefineRoom && string.Equals(rule.Category, normalized, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(rule => rule.Category, StringComparer.OrdinalIgnoreCase)
                .Select(rule => rule.Category)
                .ToArray();
        }

        public static IReadOnlyList<string> StarterRoomPriority()
        {
            return new[] { "Bedroom", "Bathroom", "Kitchen", "Living Room" };
        }

        public static string NormalizeRoomName(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                return null;
            }

            var cleaned = roomName.Trim();
            if (cleaned.IndexOf("Bedroom", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Chambre", StringComparison.OrdinalIgnoreCase) >= 0) return "Bedroom";
            if (cleaned.IndexOf("Kitchen", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Cuisine", StringComparison.OrdinalIgnoreCase) >= 0) return "Kitchen";
            if (cleaned.IndexOf("Bathroom", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Salle de bain", StringComparison.OrdinalIgnoreCase) >= 0) return "Bathroom";
            if (cleaned.IndexOf("Living Room", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("LivingRoom", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Salon", StringComparison.OrdinalIgnoreCase) >= 0) return "Living Room";
            if (cleaned.IndexOf("Outdoor", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Exterieur", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Extérieur", StringComparison.OrdinalIgnoreCase) >= 0) return "Outdoor";
            if (cleaned.IndexOf("Cultural", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Culture", StringComparison.OrdinalIgnoreCase) >= 0) return "Cultural";
            if (cleaned.IndexOf("Seating", StringComparison.OrdinalIgnoreCase) >= 0) return "Seating";
            if (cleaned.IndexOf("Decoration", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Decor", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Décor", StringComparison.OrdinalIgnoreCase) >= 0) return "Decoration";
            if (cleaned.IndexOf("Lighting", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Light", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Éclairage", StringComparison.OrdinalIgnoreCase) >= 0 || cleaned.IndexOf("Eclairage", StringComparison.OrdinalIgnoreCase) >= 0) return "Lighting";
            if (cleaned.IndexOf("Industrial", StringComparison.OrdinalIgnoreCase) >= 0) return "Industrial";
            return cleaned;
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

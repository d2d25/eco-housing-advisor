#if ECO_MODKIT
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Eco.Gameplay.Components;
using Eco.Gameplay.Components.Store;
using Eco.Gameplay.Items;
using Eco.Gameplay.Objects;
using Eco.Gameplay.Players;
using Eco.Gameplay.Skills;
using Eco.Gameplay.Systems;
using Eco.Shared.Utils;
using EcoHousingAdvisor.Domain;

namespace EcoHousingAdvisor.EcoRuntime
{
    public sealed class EcoAvailabilityReader
    {
        public HousingAvailabilitySnapshot ReadAvailability(User user, IEnumerable<HousingFurnitureItem> furniture)
        {
            var itemTypeNames = new HashSet<string>(furniture.Select(item => item.ItemTypeName), StringComparer.Ordinal);
            var itemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(type => type != null && itemTypeNames.Contains(type.Name))
                .GroupBy(type => type.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            var offers = ReadStoreOffers(user, itemTypeNames);
            var craftHints = ReadCraftHints(itemTypes);
            var result = itemTypeNames.ToDictionary(
                name => name,
                name => new HousingItemAvailability(
                    name,
                    offers.TryGetValue(name, out var itemOffers) ? itemOffers : new List<HousingStoreOffer>(),
                    craftHints.TryGetValue(name, out var itemCrafts) ? itemCrafts : new List<HousingCraftHint>()),
                StringComparer.Ordinal);

            return new HousingAvailabilitySnapshot(result);
        }

        private static Dictionary<string, List<HousingStoreOffer>> ReadStoreOffers(User user, HashSet<string> itemTypeNames)
        {
            var offers = new Dictionary<string, List<HousingStoreOffer>>(StringComparer.Ordinal);

            foreach (var store in WorldObjectUtil.AllObjsWithComponent<StoreComponent>()
                .Where(store => store != null
                    && store.Currency != null
                    && store.Parent != null
                    && store.Enabled))
            {
                foreach (var tradeOffer in store.StoreData.SellOffers.Where(offer => offer?.Stack?.Item != null && offer.Stack.Quantity > 0))
                {
                    var itemTypeName = tradeOffer.Stack.Item.GetType().Name;
                    if (!itemTypeNames.Contains(itemTypeName))
                    {
                        continue;
                    }

                    if (!offers.TryGetValue(itemTypeName, out var itemOffers))
                    {
                        itemOffers = new List<HousingStoreOffer>();
                        offers.Add(itemTypeName, itemOffers);
                    }

                    itemOffers.Add(new HousingStoreOffer(
                        ReadName(store.Parent),
                        ReadString(store.Parent, "NameOfCreator") ?? "unknown seller",
                        Convert.ToDouble(tradeOffer.Price, CultureInfo.InvariantCulture),
                        ReadName(store.Currency),
                        Convert.ToDouble(tradeOffer.Stack.Quantity, CultureInfo.InvariantCulture)));
                }
            }

            foreach (var itemOffers in offers.Values)
            {
                itemOffers.Sort((left, right) => left.Price.CompareTo(right.Price));
            }

            return offers;
        }

        private static Dictionary<string, List<HousingCraftHint>> ReadCraftHints(IReadOnlyDictionary<string, Type> itemTypes)
        {
            var result = new Dictionary<string, List<HousingCraftHint>>(StringComparer.Ordinal);
            foreach (var entry in itemTypes)
            {
                var recipes = CraftingComponent.RecipesForItem(entry.Value);
                if (recipes == null)
                {
                    continue;
                }

                foreach (var recipe in recipes.Take(2))
                {
                    var hints = recipe.RequiredSkills
                        .Select(skill => new HousingCraftHint(
                            SkillName(skill.SkillType),
                            skill.Level,
                            FindCrafters(skill.SkillType, skill.Level)))
                        .ToArray();

                    if (hints.Length == 0)
                    {
                        hints = new[] { new HousingCraftHint("No skill", 0, new string[0]) };
                    }

                    if (!result.TryGetValue(entry.Key, out var itemHints))
                    {
                        itemHints = new List<HousingCraftHint>();
                        result.Add(entry.Key, itemHints);
                    }

                    itemHints.AddRange(hints);
                }
            }

            return result;
        }

        private static IReadOnlyList<string> FindCrafters(Type skillType, int level)
        {
            return UserManager.Users
                .Where(user => HasSkillLevel(user, skillType, level))
                .Select(user => user.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        }

        private static bool HasSkillLevel(User user, Type skillType, int level)
        {
            foreach (var skill in user.Skillset.Skills)
            {
                var currentType = skill.GetType();
                if (currentType != skillType && !skillType.IsAssignableFrom(currentType))
                {
                    continue;
                }

                var currentLevel = ReadInt(skill, "Level") ?? ReadInt(skill, "TalentLevel") ?? ReadInt(skill, "CurrentLevel");
                return currentLevel != null && currentLevel.Value >= level;
            }

            return false;
        }

        private static string SkillName(Type skillType)
        {
            var name = skillType.Name;
            return name.EndsWith("Skill", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Skill".Length)
                : name;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null).Cast<Type>();
            }
        }

        private static string ReadName(object instance)
        {
            return ReadString(instance, "Name")
                ?? ReadString(instance, "DisplayName")
                ?? ReadString(instance, "MarkedUpName")
                ?? instance.GetType().Name;
        }

        private static string ReadString(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            return value == null ? null : value.ToString();
        }

        private static int? ReadInt(object instance, string memberName)
        {
            var value = ReadMember(instance, memberName);
            if (value == null)
            {
                return null;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                return null;
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static object ReadMember(object instance, string memberName)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var property = instance.GetType().GetProperty(memberName, Flags);
            if (property != null) return property.GetValue(instance);

            var field = instance.GetType().GetField(memberName, Flags);
            return field == null ? null : field.GetValue(instance);
        }
    }
}
#endif

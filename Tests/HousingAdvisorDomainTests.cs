using EcoHousingAdvisor.Domain;
using EcoHousingAdvisor.EcoRuntime;
using EcoHousingAdvisor.Presentation;

namespace EcoHousingAdvisor.Tests;

public static class HousingAdvisorDomainTests
{
    public static void Main()
    {
        GroupsByCategoryBaseValueTypeLimitAndDuplicateMultiplier();
        IgnoresZeroValueFurniture();
        RendersSimpleGroupedOutput();
        FiltersCategoryAndPaginates();
        SearchesFurnitureNames();
        RendersCompactPagedOutput();
        RendersEndOfResults();
        RendersHelp();
        FormatsTooltipText();
        MapsSupportCategoriesToUsefulRooms();
        RendersResidenceProbeWithTierCaps();
        SuggestsStoreOffersForCategory();
        SuggestsCraftHintsWhenNoStoreOfferExists();
        SuggestsNoSkillCraftsWithoutKnownCrafter();
        HidesUnavailableSuggestions();
        ReadsFakePropertyValueRooms();
        RendersPropertyValueTooltipSummary();
        FindsUsefulCategoriesForFrenchRoomNames();
        SuggestsPropertyAdditionsForWeakRooms();
        SuggestsStarterBedroomWhenPropertyHasNoRooms();
        SuggestsMissingBathroomWhenBedroomExists();
        HidesStarterBathroomUntilPrimaryRoomsHaveValue();
        AppliesDuplicatePenaltyFromMappedRoomTypes();
        ReadsFakeRoomFurnitureTypeLimits();
        ReadsFakePropertyValueRoomsObjectsAndDescriptions();
        RendersGlobalRoomCommands();
        AppliesBedDuplicatePenaltyFromGlobalResidence();
        HidesUnavailablePropertyAdvice();
        DoesNotApplyFinalPropertyMultiplierToDelta();
        AppliesSupportCategoryCap();
        AppliesBathroomPropertyCap();
        PreservesNativeLinkTargetsForRichTooltips();
        Console.WriteLine("EcoHousingAdvisor fake domain tests passed.");
    }

    private static void GroupsByCategoryBaseValueTypeLimitAndDuplicateMultiplier()
    {
        var items = new[]
        {
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Hardwood Chair", "Seating", 2, "Chair", 0.5),
            Item("Tiny Rug", "Decoration", 1, "Rug", 0.25),
            Item("Fancy Chair", "Seating", 3, "Chair", 0.5),
        };

        var groups = new HousingFurnitureGrouper().GroupFurniture(items);

        AssertEqual(3, groups.Count, "group count");
        var seatingChair = groups.Single(group => group.Category == "Seating" && group.BaseValue == 2);
        AssertEqual("Chair", seatingChair.TypeForRoomLimit, "type limit");
        AssertEqual(0.5, seatingChair.DiminishingReturnMultiplier, "duplicate multiplier");
        AssertEqual(2, seatingChair.Items.Count, "grouped item count");
    }

    private static void IgnoresZeroValueFurniture()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("No Value", "Decoration", 0, "None", null),
            Item("Useful Lamp", "Lighting", 1.5, "Light", 0.4),
        ]);

        AssertEqual(1, groups.Count, "positive value group count");
        AssertEqual("Lighting", groups[0].Category, "remaining category");
    }

    private static void RendersSimpleGroupedOutput()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
        ]);

        var output = new AdvisorTextRenderer().RenderFurnitureGroups(groups);

        AssertContains("Eco Housing Advisor: 1 furniture entries in 1 groups.", output);
        AssertContains("Seating", output);
        AssertContains("base 2", output);
        AssertContains("type limit Chair", output);
        AssertContains("duplicate multiplier 0.5", output);
    }

    private static void FiltersCategoryAndPaginates()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Wooden Table", "Seating", 1, "Table", 0.2),
            Item("Basic Lamp", "Lighting", 1, "Light", 0.4),
        ]);

        var result = new HousingFurnitureBrowser().Query(
            groups,
            new HousingFurnitureQuery("category", "Seating", 1, 1));

        AssertEqual(2, result.FilteredGroupCount, "filtered seating group count");
        AssertEqual(1, result.Groups.Count, "page group count");
        AssertEqual(2, result.PageCount, "page count");
        AssertEqual("Seating", result.Groups[0].Category, "page category");
    }

    private static void SearchesFurnitureNames()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Wooden Table", "Seating", 1, "Table", 0.2),
            Item("Basic Lamp", "Lighting", 1, "Light", 0.4),
        ]);

        var result = new HousingFurnitureBrowser().Query(
            groups,
            new HousingFurnitureQuery("search", "lamp", 1, 8));

        AssertEqual(1, result.FilteredGroupCount, "search group count");
        AssertEqual("Lighting", result.Groups[0].Category, "search category");
        AssertEqual("Basic Lamp", result.Groups[0].Items[0].DisplayName, "search item");
    }

    private static void RendersCompactPagedOutput()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Wooden Table", "Seating", 1, "Table", 0.2),
        ]);
        var result = new HousingFurnitureBrowser().Query(
            groups,
            new HousingFurnitureQuery("summary", null, 1, 1));

        var output = new AdvisorTextRenderer().RenderFurnitureResult(result);

        AssertContains("Showing 1/2 groups, page 1/2", output);
        AssertContains("Next: /housingadvisor list 2", output);
    }

    private static void RendersEndOfResults()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Wooden Table", "Seating", 1, "Table", 0.2),
        ]);
        var result = new HousingFurnitureBrowser().Query(
            groups,
            new HousingFurnitureQuery("summary", null, 2, 1));

        var output = new AdvisorTextRenderer().RenderFurnitureResult(result);

        AssertContains("Showing 1/2 groups, page 2/2", output);
        AssertContains("End of results.", output);
    }

    private static void RendersHelp()
    {
        var output = new AdvisorTextRenderer().RenderHelp();

        AssertContains("/housingadvisor list", output);
        AssertContains("/housingadvisor list 2", output);
        AssertContains("/housingadvisor category Seating", output);
        AssertContains("/housingadvisor search chair", output);
        AssertContains("/housingadvisor hahelp", output);
    }

    private static void FormatsTooltipText()
    {
        var output = HousingFurnitureFormatter.FormatTooltip(Item("Hewn Chair", "Seating", 2, "Chair", 0.5));

        AssertContains("Category: Seating", output);
        AssertContains("Base value: 2", output);
        AssertContains("Type limit: Chair", output);
        AssertContains("Duplicate multiplier: 0.5", output);
    }

    private static void MapsSupportCategoriesToUsefulRooms()
    {
        var seating = HousingRoomRules.ForCategory("Seating");
        AssertEqual(false, seating.CanDefineRoom, "seating room category");
        AssertContains("Living Room", HousingRoomRules.FormatUsefulRooms("Seating"));
        AssertContains("Bedroom", HousingRoomRules.FormatUsefulRooms("Seating"));

        var lighting = HousingRoomRules.ForCategory("Lighting");
        AssertEqual(true, lighting.SupportForAnyRoom, "lighting any-room support");
        AssertContains("Kitchen", HousingRoomRules.FormatUsefulRooms("Lighting"));

        AssertContains("Bedroom", HousingRoomRules.FormatUsefulRooms("Living Room"));
        AssertContains("Living Room", HousingRoomRules.FormatUsefulRooms("Cultural"));
        AssertContains("Outdoor", HousingRoomRules.FormatUsefulRooms("Cultural"));

        var industrial = HousingRoomRules.ForCategory("Industrial");
        AssertContains("avoid on residence", industrial.Note);
    }

    private static void RendersResidenceProbeWithTierCaps()
    {
        var snapshot = new HousingResidenceSnapshot(
            "Ada",
            [
                new HousingRoomSnapshot("Bedroom", "Bedroom", 2, 8.4, 6, true, "fake"),
            ],
            ["Full residence room enumeration is not confirmed yet."],
            DateTimeOffset.UtcNow);

        var output = new AdvisorTextRenderer().RenderResidence(snapshot);

        AssertContains("residence probe for Ada", output);
        AssertContains("Bedroom: category Bedroom, tier 2, value 8.4, furniture 6, contained yes", output);
        AssertContains("caps: soft 10, hard 20, diminishing 0.65", output);
        AssertContains("Note: Full residence room enumeration is not confirmed yet.", output);
    }

    private static void SuggestsStoreOffersForCategory()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 1, "Chair", 0.7),
            Item("Fancy Chair", "Seating", 3, "Chair", 0.5),
        ]);
        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["FancyChairItem"] = new HousingItemAvailability(
                "FancyChairItem",
                [new HousingStoreOffer("Best Furniture", "Ada", 12, "Credits", 4)],
                []),
        });

        var result = new HousingSuggestionEngine().SuggestByCategory(groups, availability, "Seating", 1, 5);
        var output = new AdvisorTextRenderer().RenderSuggestions(result);

        AssertContains("best additions for Seating", output);
        AssertContains("Fancy Chair", output);
        AssertContains("+3 XP/day", output);
        AssertContains("useful in Living Room, Bedroom, Kitchen, Bathroom, Outdoor, Cultural", output);
        AssertContains("Buy: 12 Credits at Best Furniture", output);
        AssertContains("seller Ada", output);
    }

    private static void SuggestsCraftHintsWhenNoStoreOfferExists()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 1, "Chair", 0.7),
        ]);
        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["HewnChairItem"] = new HousingItemAvailability(
                "HewnChairItem",
                [],
                [new HousingCraftHint("Logging", 1, ["Ada", "Ben"])]),
        });

        var result = new HousingSuggestionEngine().SuggestByCategory(groups, availability, "Seating", 1, 5);
        var output = new AdvisorTextRenderer().RenderSuggestions(result);

        AssertContains("Hewn Chair", output);
        AssertContains("Craft: Logging level 1; crafters: Ada, Ben", output);
    }

    private static void SuggestsNoSkillCraftsWithoutKnownCrafter()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Stump Bed", "Bedroom", 1, "Bed", 0.5),
        ]);
        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["StumpBedItem"] = new HousingItemAvailability(
                "StumpBedItem",
                [],
                [new HousingCraftHint("No skill", 0, [], true)]),
        });

        var result = new HousingSuggestionEngine().SuggestByCategory(groups, availability, "Bedroom", 1, 5);
        var output = new AdvisorTextRenderer().RenderSuggestions(result);

        AssertContains("Stump Bed", output);
        AssertContains("Craft: no skill required", output);
    }

    private static void HidesUnavailableSuggestions()
    {
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Ecko Statue", "Decoration", 5, "Statue", 0.5),
            Item("Buyable Painting", "Decoration", 3, "Painting", 0.5),
        ]);
        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["BuyablePaintingItem"] = new HousingItemAvailability(
                "BuyablePaintingItem",
                [new HousingStoreOffer("Decor Shop", "Ada", 10, "Credits", 1)],
                []),
        });

        var result = new HousingSuggestionEngine().SuggestByCategory(groups, availability, "Decoration", 1, 5);
        var output = new AdvisorTextRenderer().RenderSuggestions(result);

        AssertContains("Buyable Painting", output);
        AssertNotContains("Ecko Statue", output);
    }

    private static void ReadsFakePropertyValueRooms()
    {
        var fake = new FakePropertyValue
        {
            TotalValue = 21.2,
            RoomValues = new Dictionary<string, FakeRoomValue>
            {
                ["Bedroom"] = new FakeRoomValue { Value = 8.4 },
                ["Kitchen"] = new FakeRoomValue { Value = 12.8 },
            },
        };

        var snapshot = new EcoPropertyValueReader().Read(fake);

        AssertEqual(21.2, snapshot.TotalValue, "property total");
        AssertEqual(2, snapshot.Rooms.Count, "property room count");
        AssertContains("Bedroom", snapshot.Rooms[0].RoomName);
        AssertEqual(8.4, snapshot.Rooms[0].Value, "bedroom value");
    }

    private static void RendersPropertyValueTooltipSummary()
    {
        var snapshot = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            21.2,
            1,
            1,
            [new HousingPropertyRoomValue("Bedroom", "Bedroom", 8.4, 2)],
            [],
            DateTimeOffset.UtcNow);

        var output = new AdvisorTextRenderer().RenderPropertyValue(snapshot);

        AssertContains("Best useful additions (1 residence rooms):", output);
        AssertContains("- Bedroom: 8.4 XP/day, tier 2", output);
        AssertContains("Total read: 21.2 XP/day", output);
    }

    private static void FindsUsefulCategoriesForFrenchRoomNames()
    {
        var salonCategories = HousingRoomRules.CategoriesUsefulInRoom("Salon");
        AssertContains("Living Room", string.Join(", ", salonCategories));
        AssertContains("Seating", string.Join(", ", salonCategories));
        AssertContains("Decoration", string.Join(", ", salonCategories));

        var bathroomCategories = HousingRoomRules.CategoriesUsefulInRoom("Salle de bain");
        AssertContains("Bathroom", string.Join(", ", bathroomCategories));
        AssertContains("Lighting", string.Join(", ", bathroomCategories));
    }

    private static void SuggestsPropertyAdditionsForWeakRooms()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            30,
            1,
            1,
            [
                new HousingPropertyRoomValue("Salon", "Salon", 6.3, 2),
                new HousingPropertyRoomValue("Cuisine", "Cuisine", 11.5, 2),
            ],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Hewn Chair", "Seating", 2, "Chair", 0.5),
            Item("Big Painting", "Decoration", 5, "Painting", 0.5),
            Item("Table Lamp", "Lighting", 3, "Lamp", 0.5),
            Item("Small Couch", "Living Room", 4, "Couch", 0.5),
        ]);
        var allAvailable = AvailabilityFor(groups);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, allAvailable, 1, 3);
        AssertEqual(1, advice.Rooms.Count, "advice room count");
        AssertEqual("Salon", advice.Rooms[0].Room.RoomName, "weakest room");
        AssertContains("Big Painting", advice.Rooms[0].Additions[0].Group.Items[0].DisplayName);
        AssertEqual(3.7, advice.Rooms[0].Additions[0].EstimatedGain, "soft cap bounded gain");
        AssertEqual("before soft cap", advice.Rooms[0].Additions[0].CapNote, "soft cap note");

        var output = new AdvisorTextRenderer().RenderPropertyValue(property, groups, allAvailable);
        AssertContains("Salon 6.3 XP/day:", output);
        AssertContains("Big Painting in Salon", output);
        AssertContains("before soft cap", output);

        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["BigPaintingItem"] = new HousingItemAvailability(
                "BigPaintingItem",
                [new HousingStoreOffer("Decor Shop", "Ada", 42, "Credits", 1)],
                []),
        });
        var outputWithAvailability = new AdvisorTextRenderer().RenderPropertyValue(property, groups, availability);
        AssertContains("Buy: 42 Credits at Decor Shop", outputWithAvailability);

        var cappedProperty = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            30,
            1,
            1,
            [new HousingPropertyRoomValue("Salon", "Salon", 12, 2)],
            [],
            DateTimeOffset.UtcNow);
        var cappedAdvice = new HousingPropertyAdviceEngine().BuildAdvice(cappedProperty, groups, allAvailable, 1, 1);
        AssertEqual(3.25, cappedAdvice.Rooms[0].Additions[0].EstimatedGain, "diminished gain");
        AssertEqual("past soft cap", cappedAdvice.Rooms[0].Additions[0].CapNote, "diminished cap note");
    }

    private static void SuggestsStarterBedroomWhenPropertyHasNoRooms()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            0,
            1,
            1,
            [],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Stump Bed", "Bedroom", 1, "Bed", 0.5),
            Item("Unavailable Latrine", "Bathroom", 1, "Toilet", 0.5),
        ]);
        var availability = new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>
        {
            ["StumpBedItem"] = new HousingItemAvailability(
                "StumpBedItem",
                [],
                [new HousingCraftHint("No skill", 0, [], true)]),
        });

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, availability, 2, 2);
        AssertEqual(1, advice.NewRooms.Count, "starter room count");
        AssertEqual("Bedroom", advice.NewRooms[0].Room.Category, "starter room category");
        AssertContains("Stump Bed", advice.NewRooms[0].Additions[0].Group.Items[0].DisplayName);

        var output = new AdvisorTextRenderer().RenderPropertyValue(property, groups, availability);
        AssertContains("No residence rooms found yet.", output);
        AssertContains("New useful room setups:", output);
        AssertContains("Bedroom:", output);
        AssertContains("Stump Bed", output);
        AssertContains("Craft: no skill required", output);
    }

    private static void SuggestsMissingBathroomWhenBedroomExists()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            1,
            1,
            1,
            [new HousingPropertyRoomValue("Chambre", "Bedroom", 1, 1)],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Stump Bed", "Bedroom", 1, "Bed", 0.5),
            Item("Latrine", "Bathroom", 2, "Toilet", 0.5),
        ]);
        var availability = AvailabilityFor(groups);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, availability, 2, 2);

        AssertEqual(1, advice.NewRooms.Count, "missing bathroom room count");
        AssertEqual("Bathroom", advice.NewRooms[0].Room.Category, "missing room category");
        AssertContains("Latrine", advice.NewRooms[0].Additions[0].Group.Items[0].DisplayName);
        AssertEqual(0.33, Math.Round(advice.NewRooms[0].Additions[0].EstimatedGain, 2), "missing bathroom property cap");
    }

    private static void HidesStarterBathroomUntilPrimaryRoomsHaveValue()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            0,
            1,
            1,
            [],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Stump Bed", "Bedroom", 2.5, "Bed", 0.4),
            Item("Stump Latrine", "Bathroom", 1.5, "Toilet", 0.4),
        ]);
        var availability = AvailabilityFor(groups);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, availability, 2, 2);

        AssertEqual(1, advice.NewRooms.Count, "starter advice should only include primary rooms with final gain");
        AssertEqual("Bedroom", advice.NewRooms[0].Room.Category, "starter advice first useful room");
        AssertNotContains("Bathroom", string.Join(", ", advice.NewRooms.Select(room => room.Room.Category)));
    }

    private static void AppliesDuplicatePenaltyFromMappedRoomTypes()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            12,
            1,
            1,
            [
                new HousingPropertyRoomValue(
                    "Salon",
                    "Salon",
                    1,
                    2,
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Chair"] = 1 }),
            ],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Great Chair", "Seating", 8, "Chair", 0.25),
            Item("Small Couch", "Living Room", 4, "Couch", 0.5),
        ]);
        var allAvailable = AvailabilityFor(groups);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, allAvailable, 1, 2);

        AssertEqual("Small Couch", advice.Rooms[0].Additions[0].Group.Items[0].DisplayName, "duplicate adjusted winner");
        AssertEqual("Great Chair", advice.Rooms[0].Additions[1].Group.Items[0].DisplayName, "duplicate adjusted loser");
        AssertEqual(1, advice.Rooms[0].Additions[1].ExistingTypeCount, "existing type count");
        AssertEqual(0.25, advice.Rooms[0].Additions[1].DuplicateFactor, "duplicate factor");

        var output = new AdvisorTextRenderer().RenderPropertyValue(property, groups, allAvailable);
        AssertContains("mapped types: Chair x1", output);
        AssertContains("duplicate type x1, factor 0.25", output);
    }

    private static void ReadsFakeRoomFurnitureTypeLimits()
    {
        var fake = new FakePropertyValue
        {
            TotalValue = 8,
            RoomValues = new Dictionary<string, FakeRoomValue>
            {
                ["Salon"] = new FakeRoomValue
                {
                    Value = 8,
                    Furniture = [new FakeFurniture { HomeValue = new FakeHomeValue { TypeForRoomLimit = "Chair" } }],
                },
            },
        };

        var snapshot = new EcoPropertyValueReader().Read(fake);

        AssertEqual(1, snapshot.Rooms[0].CountExistingType("Chair"), "mapped chair count");
    }

    private static void ReadsFakePropertyValueRoomsObjectsAndDescriptions()
    {
        var fake = new FakePropertyValue
        {
            TotalValue = 2.5,
            Rooms =
            [
                new FakeRoom
                {
                    Name = "Bedroom",
                    RoomValue = new FakeRoomValue { Value = 2.5, Description = "Stump Bed +2.5" },
                    RoomStats = new FakeRoomStats
                    {
                        AverageTier = 1,
                        ContainedWorldObjects =
                        [
                            new FakeFurniture
                            {
                                Name = "Stump Bed",
                                HomeValue = new FakeHomeValue
                                {
                                    Category = "Bedroom",
                                    TypeForRoomLimit = "Bed",
                                    BaseValue = 2.5,
                                    DiminishingReturnMultiplier = 0.4,
                                },
                            },
                        ],
                    },
                },
            ],
        };

        var snapshot = new EcoPropertyValueReader().Read(fake);

        AssertEqual(1, snapshot.Rooms.Count, "global room count");
        AssertEqual(1, snapshot.Rooms[0].CountExistingType("Bed"), "global bed count");
        AssertEqual("Stump Bed", snapshot.Rooms[0].Objects[0].DisplayName, "room object name");
        AssertEqual("Stump Bed +2.5", snapshot.Rooms[0].EcoDescription, "room eco detail");
    }

    private static void RendersGlobalRoomCommands()
    {
        var snapshot = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            2.5,
            1,
            1,
            [
                new HousingPropertyRoomValue(
                    "Bedroom",
                    "Bedroom",
                    2.5,
                    1,
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Bed"] = 1 },
                    null,
                    [
                        new HousingPropertyRoomObjectValue("Stump Bed", "StumpBedItem", "Bedroom", "Bed", 2.5, 0.4, 2.5, true),
                    ],
                    "Stump Bed +2.5"),
            ],
            [],
            DateTimeOffset.UtcNow);

        var renderer = new AdvisorTextRenderer();
        var roomsOutput = renderer.RenderRooms(snapshot);
        var roomOutput = renderer.RenderRoomDetails(snapshot, "Bedroom");

        AssertContains("Total Eco: 2.5 XP/day", roomsOutput);
        AssertContains("furniture types: Bed x1", roomsOutput);
        AssertContains("Stump Bed", roomOutput);
        AssertContains("Next Bed: duplicate multiplier 0.4, existing x1.", roomOutput);
    }

    private static void AppliesBedDuplicatePenaltyFromGlobalResidence()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            2.5,
            1,
            1,
            [
                new HousingPropertyRoomValue(
                    "Bedroom",
                    "Bedroom",
                    2.5,
                    1,
                    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Bed"] = 1 }),
            ],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Stump Bed", "Bedroom", 2.5, "Bed", 0.4),
        ]);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, AvailabilityFor(groups), 1, 1);

        AssertEqual(1.0, advice.Rooms[0].Additions[0].EstimatedGain, "second bed duplicate gain");
    }

    private static void HidesUnavailablePropertyAdvice()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            12,
            1,
            1,
            [new HousingPropertyRoomValue("Salon", "Salon", 1, 2)],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Unavailable Painting", "Decoration", 5, "Painting", 0.5),
        ]);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(
            property,
            groups,
            new HousingAvailabilitySnapshot(new Dictionary<string, HousingItemAvailability>()),
            1,
            3);

        AssertEqual(0, advice.Rooms.Count, "unavailable advice hidden");
    }

    private static void DoesNotApplyFinalPropertyMultiplierToDelta()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            24,
            2,
            2,
            [new HousingPropertyRoomValue("Salon", "Salon", 1, 2)],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Small Couch", "Living Room", 4, "Couch", 0.5),
        ]);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, AvailabilityFor(groups), 1, 1);

        AssertEqual(4, advice.Rooms[0].Additions[0].EstimatedGain, "final multiplier ignored for delta");
    }

    private static void AppliesSupportCategoryCap()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            20,
            1,
            1,
            [
                new HousingPropertyRoomValue(
                    "Salon",
                    "Living Room",
                    8,
                    3,
                    null,
                    new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Living Room"] = 8,
                        ["Decoration"] = 3.5,
                    }),
            ],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Big Painting", "Decoration", 5, "Painting", 0.5),
        ]);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, AvailabilityFor(groups), 1, 1);

        AssertEqual(0.5, advice.Rooms[0].Additions[0].EstimatedGain, "support cap remaining");
    }

    private static void AppliesBathroomPropertyCap()
    {
        var property = new HousingPropertyValueSnapshot(
            "FakePropertyValue",
            20,
            1,
            1,
            [
                new HousingPropertyRoomValue("Salon", "Living Room", 10, 3),
                new HousingPropertyRoomValue("Salle de bain", "Bathroom", 3, 3),
            ],
            [],
            DateTimeOffset.UtcNow);
        var groups = new HousingFurnitureGrouper().GroupFurniture(
        [
            Item("Fancy Toilet", "Bathroom", 5, "Toilet", 0.5),
        ]);

        var advice = new HousingPropertyAdviceEngine().BuildAdvice(property, groups, AvailabilityFor(groups), 2, 1);

        AssertEqual(0.3, Math.Round(advice.Rooms[0].Additions[0].EstimatedGain, 2), "bathroom cap remaining");
    }

    private static void PreservesNativeLinkTargetsForRichTooltips()
    {
        var room = new HousingPropertyRoomValue(
            "Bedroom",
            "Bedroom",
            2.5,
            1,
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["Bed"] = 1 },
            null,
            [
                new HousingPropertyRoomObjectValue(
                    "Stump Bed",
                    "StumpBedItem",
                    "Bedroom",
                    "Bed",
                    2.5,
                    0.4,
                    2.5,
                    true,
                    new HousingLinkTarget(HousingLinkTargetKind.WorldObject, "Stump Bed", "StumpBedObject", 42)),
            ],
            "Stump Bed +2.5",
            HousingLinkTarget.RoomCategory("Bedroom"));

        var availability = new HousingItemAvailability(
            "StumpBedItem",
            [
                new HousingOwnedItemLocation(
                    "d2d25's Campsite",
                    false,
                    1,
                    new HousingLinkTarget(HousingLinkTargetKind.Storage, "d2d25's Campsite", "StockpileObject", 5)),
            ],
            [
                new HousingStoreOffer(
                    "Bed Shop",
                    "Ada",
                    10,
                    "Credits",
                    1,
                    new HousingLinkTarget(HousingLinkTargetKind.Store, "Bed Shop", "StoreObject", 6),
                    new HousingLinkTarget(HousingLinkTargetKind.User, "Ada"),
                    new HousingLinkTarget(HousingLinkTargetKind.Currency, "Credits", "CreditItem")),
            ],
            [
                new HousingCraftHint(
                    "Logging",
                    1,
                    ["Ada"],
                    false,
                    new HousingLinkTarget(HousingLinkTargetKind.Skill, "Logging", "LoggingSkill"),
                    new HousingLinkTarget(HousingLinkTargetKind.Recipe, "Stump Bed Recipe", "StumpBedRecipe"),
                    [new HousingLinkTarget(HousingLinkTargetKind.User, "Ada")]),
            ]);

        AssertEqual(HousingLinkTargetKind.RoomCategory, room.RoomCategoryLink.Kind, "room category link kind");
        AssertEqual("Bedroom", room.RoomCategoryLink.TypeName, "room category link type");
        AssertEqual(HousingLinkTargetKind.WorldObject, room.Objects[0].ObjectLink.Kind, "placed furniture link kind");
        AssertEqual(HousingLinkTargetKind.Storage, availability.OwnedLocations[0].LocationLink.Kind, "storage link kind");
        AssertEqual(HousingLinkTargetKind.Store, availability.StoreOffers[0].StoreLink.Kind, "store link kind");
        AssertEqual(HousingLinkTargetKind.User, availability.StoreOffers[0].SellerLink.Kind, "seller link kind");
        AssertEqual(HousingLinkTargetKind.Currency, availability.StoreOffers[0].CurrencyLink.Kind, "currency link kind");
        AssertEqual(HousingLinkTargetKind.Skill, availability.CraftHints[0].SkillLink.Kind, "skill link kind");
        AssertEqual(HousingLinkTargetKind.User, availability.CraftHints[0].CrafterLinks[0].Kind, "crafter link kind");
    }

    private static HousingFurnitureItem Item(string name, string category, double baseValue, string typeLimit, double? duplicateMultiplier)
    {
        return new HousingFurnitureItem(
            $"{name.Replace(" ", string.Empty)}Item",
            name,
            category,
            baseValue,
            typeLimit,
            duplicateMultiplier);
    }

    private static HousingAvailabilitySnapshot AvailabilityFor(IReadOnlyList<HousingFurnitureGroup> groups)
    {
        return new HousingAvailabilitySnapshot(groups.ToDictionary(
            group => group.Items[0].ItemTypeName,
            group => new HousingItemAvailability(
                group.Items[0].ItemTypeName,
                [new HousingStoreOffer("Test Shop", "Ada", 1, "Credits", 1)],
                [])));
    }

    private sealed class FakePropertyValue
    {
        public double TotalValue { get; set; }

        public Dictionary<string, FakeRoomValue> RoomValues { get; set; } = new Dictionary<string, FakeRoomValue>();

        public List<FakeRoom> Rooms { get; set; } = new List<FakeRoom>();
    }

    private sealed class FakeRoom
    {
        public string Name { get; set; }

        public FakeRoomValue RoomValue { get; set; } = new FakeRoomValue();

        public FakeRoomStats RoomStats { get; set; } = new FakeRoomStats();
    }

    private sealed class FakeRoomValue
    {
        public double Value { get; set; }

        public string Description { get; set; }

        public List<FakeFurniture> Furniture { get; set; } = new List<FakeFurniture>();
    }

    private sealed class FakeRoomStats
    {
        public double AverageTier { get; set; }

        public List<FakeFurniture> ContainedWorldObjects { get; set; } = new List<FakeFurniture>();
    }

    private sealed class FakeFurniture
    {
        public string Name { get; set; }

        public FakeHomeValue HomeValue { get; set; } = new FakeHomeValue();
    }

    private sealed class FakeHomeValue
    {
        public string Category { get; set; }

        public string TypeForRoomLimit { get; set; }

        public double BaseValue { get; set; }

        public double DiminishingReturnMultiplier { get; set; } = 1;
    }

    private static void AssertEqual<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}.");
        }
    }

    private static void AssertContains(string expected, string actual)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected output to contain '{expected}'. Actual output: {actual}");
        }
    }

    private static void AssertNotContains(string unexpected, string actual)
    {
        if (actual.Contains(unexpected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Expected output to not contain '{unexpected}'. Actual output: {actual}");
        }
    }
}

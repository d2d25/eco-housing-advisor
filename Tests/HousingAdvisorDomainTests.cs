using EcoHousingAdvisor.Domain;
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
}

# Eco Housing Advisor

Eco Housing Advisor is an Eco server mod that helps players understand which housing furniture is worth placing next.

The player-facing experience is intentionally tooltip-first:

- hover a housing item to see its best possible gain on the current residence;
- hover `Residency Property Value` to see the best useful additions for the property;
- use Eco-native links and foldouts wherever possible, so item, shop, storage, player, and room details behave like normal Eco UI.

Chat commands are admin diagnostics only. They are kept for validation, debugging, and server support, not as the final player interface.

## Current Features

- Discovers housing furniture from Eco runtime data.
- Reads `WorldObjectItem.HomeValue` for category, base value, type limit, and duplicate multiplier.
- Reads active residence `PropertyValue` / `ResidencyPropertyValue` and its rooms when Eco exposes them.
- Scores next useful additions with duplicate penalties, support caps, room tier soft/hard caps, and property category caps.
- Filters recommendations to furniture that is available through at least one of:
  - player inventory or owned storage;
  - active store stock;
  - craft recipe with no required skill;
  - craft recipe with a known crafter.
- Keeps domain scoring separate from Eco runtime/UI code.
- Uses short TTL caches to avoid expensive hover-time scans.

## Player Usage

Players do not need commands.

Recommended checks:

1. Hover a housing furniture item.
   - Expected: `Eco Housing Advisor` shows the maximum useful XP/day it can add to the current property and the best placement.
2. Hover `Residency Property Value`.
   - Expected: the tooltip lists a few best useful additions, where to place them, and where to find them.
3. Hover linked objects inside the advisor text.
   - Expected: Eco-native tooltips or foldouts open for items, shops, storage, players, and existing room details when available.

## Admin Commands

All `/housingadvisor` commands require `ChatAuthorizationLevel.Admin`.

These commands are diagnostic/admin tools:

```text
/housingadvisor hahelp
/housingadvisor list
/housingadvisor list 2
/housingadvisor category Seating
/housingadvisor category Seating 2
/housingadvisor search chair
/housingadvisor search chair 2
/housingadvisor suggest Bedroom
/housingadvisor harooms
/housingadvisor haroom Bedroom
/housingadvisor haitem trophy
/housingadvisor hacalc trophy
/housingadvisor harules
/housingadvisor hadebug
/housingadvisor harefresh
/housingadvisor uistatus
```

Notes:

- `ha*` command names avoid collisions with Eco 0.13 built-in command keys.
- Chat output is plain text. Rich links are reserved for Eco tooltips.
- Commands may expose raw runtime/readability details and should not be treated as the final UX.
- `harules` audits the advisor's copied housing rule table against Eco runtime/source rules and should be run after Eco updates.

## Installation

From this repository:

```powershell
powershell -ExecutionPolicy Bypass -File tools\install-to-eco-server.ps1
```

Default target:

```text
C:\Program Files (x86)\Steam\steamapps\common\Eco Server\Mods\UserCode\EcoHousingAdvisor
```

Restart `EcoServer.exe` after installation.

## Validation Checklist

Run domain tests:

```powershell
dotnet run --project EcoHousingAdvisor.csproj --framework net10.0 -p:EcoHousingAdvisorTests=true
```

Install and restart the Eco server:

```powershell
powershell -ExecutionPolicy Bypass -File tools\install-to-eco-server.ps1
```

After restart, check the latest Eco log for:

- no `error CS`;
- no unexpected `Exception`;
- no unexpected `Failed`;
- `Web Server now listening`.

In-game smoke tests:

- item tooltip shows max useful property gain, not duplicate vanilla housing data;
- property value tooltip shows best useful additions;
- links/foldouts open for available Eco objects where possible;
- non-admin users cannot run `/housingadvisor`;
- admin users can run `/housingadvisor hahelp`, `list`, `search`, `category`, `suggest`, `harooms`, `haroom`, `hadebug`, and `harefresh`.

## Runtime Boundaries

Eco Housing Advisor uses Eco runtime data as source of truth whenever possible. Some Eco APIs are not stable or not fully exposed from `Mods/UserCode`, so the mod uses defensive reflection in runtime adapters and keeps uncertainty out of the domain model.

Known limits before V1:

- Utility requirements such as electricity, water, fuel, chimney, or power are not fully validated yet.
- Existing room links use Eco room value descriptions/foldouts when available; if Eco does not expose a specific room value, the tooltip falls back to the room category.
- Exact future gain is still bounded by Eco's real property state after placement; the mod estimates the delta from currently readable runtime data and avoids spawning fake residences.

## Architecture

- `Domain/`: pure scoring/query models and tests; no Eco runtime types.
- `EcoRuntime/`: Eco API/reflection readers, caches, availability, and runtime link targets.
- `UI/`: Eco tooltip integration and rich `LocString` rendering.
- `Commands/`: admin-only chat diagnostics.
- `Presentation/`: plain-text command rendering.
- `Tests/`: fake domain/runtime tests that run outside Eco.

## Credits / Inspiration

This mod was built with ideas inspired by:

- [OpenNutriView](https://github.com/BeanHeaDza/OpenNutriView), especially the tooltip-first player experience and live Eco runtime/store scanning approach.
- [FurnitureFinder](https://github.com/BeanHeaDza/FurnitureFinder), especially the furniture discovery direction around `HousingComponent`, `WorldObjectItem`, and `HomeValue`.

Thanks to BeanHeaDza and those projects for showing clean Eco mod patterns. Eco Housing Advisor does not claim affiliation and does not intentionally copy their code; it adapts the concepts to housing XP.

# Eco Housing Advisor Roadmap

## Goal For V1

V1 should feel useful in-game, like a focused housing equivalent of a food advisor:

- simple command flow;
- fast response;
- no giant chat spam;
- lightweight in-game UI where Eco supports it;
- explains the next useful furniture to add;
- uses Eco runtime data as source of truth;
- stays safe on a live server.

V1 is not the full economy optimizer. It is the first playable room advisor.

Chat is only the development and fallback interface. The final user experience should move important browsing and recommendations into Eco UI surfaces as soon as the runtime APIs are confirmed.

## Current State

V0.5 can:

- register `/housingadvisor`;
- list furniture groups from Eco runtime `HomeValue`;
- filter by category with `/housingadvisor category Seating`;
- search with `/housingadvisor search chair`;
- show debug with `/housingadvisor hadebug`;
- refresh cache with `/housingadvisor harefresh`;
- show `/housingadvisor uistatus`;
- add a lightweight furniture tooltip probe based on OpenNutriView's tooltip-library pattern;
- suggest best additions for a housing category with `/housingadvisor suggest Seating`;
- show cheapest active shop offer when available;
- show craft skill/crafter hints when no shop offer is found;
- avoid Eco attribute construction errors after startup.

## V0.3: Polish The Data Browser

Purpose: make the current probe pleasant enough for testers.

Tasks:

- Add clear help output: `/housingadvisor help`.
- Add aliases if Eco permits them safely:
  - `/ha`
  - `/ha search chair`
  - `/ha category Seating`
- Improve pagination text:
  - show current page;
  - show next command;
  - show when no more pages exist.
- Show compact item names:
  - strip `Item`;
  - split PascalCase into words;
  - keep class name in debug only.
- Add output limits for very large groups.
- Add tests for help, aliases, pagination, and display name formatting.

Acceptance:

- A new tester can type `/housingadvisor help` and understand what to try.
- Search/category output is readable in chat.
- No server restart/runtime errors.

Status:

- Implemented as `/housingadvisor hahelp` to avoid unknown Eco command-key collisions.
- Implemented class-name display formatting, next-page hints, and end-of-results text.
- Implemented summary browsing as `/housingadvisor list` and `/housingadvisor list <number>` because Eco routes root commands with sub-commands to automatic help.

## V0.4: Lightweight In-Game UI Probe

Purpose: investigate and implement the first UI surface, inspired by OpenNutriView's tooltip/config approach.

Tasks:

- Add `/housingadvisor help` if not already done.
- Add `/housingadvisor config` if Eco's popup editing APIs are safe to use.
- Investigate Eco UI APIs used by OpenNutriView:
  - tooltip hooks;
  - `ViewEditorUtils.PopupUserEditValue(...)`;
  - player-specific config storage;
  - tab/window APIs if exposed in Eco 0.13.
- Add a minimal player config:
  - max suggestions;
  - show debug details;
  - preferred output mode;
  - ignored categories.
- If tooltip hooks are safe, add furniture item tooltip lines:
  - housing category;
  - base value;
  - type limit;
  - duplicate multiplier.
- Keep chat output as fallback.

Acceptance:

- `/housingadvisor config` opens a small in-game config UI, or the README documents why it is blocked.
- Furniture tooltip enrichment works without server startup errors, or is explicitly deferred.
- The UI code stays separate from domain scoring logic.

Status:

- Implemented the furniture tooltip probe first, using the same simple `TooltipLibrary`/extension-method style as OpenNutriView.
- Confirmed server startup succeeds with the probe installed.
- Deferred config UI to the next UI slice so the tooltip can be tested in isolation.
- Documented that the advanced `TooltipOrigin` signature did not compile from this server's `Mods/UserCode` context.

## V0.5: Runtime Discovery Hardening

Purpose: make the furniture dataset trustworthy.

Tasks:

- Count and report skipped items with reasons in debug:
  - no `HomeValue`;
  - no base value;
  - dynamic/unknown value;
  - reflection read failure.
- Add safe runtime warnings list to the cached snapshot.
- Compare discovered furniture count with a sample of vanilla `__core__` files.
- Investigate a side-effect-free way to get localized display names.
- Investigate `WorldObjectItem.GetCreatingItemTemplateFromType(...)` without triggering item attribute creation.
- Keep fallback class-name display if the safe API is not confirmed.

Acceptance:

- `/housingadvisor hadebug` tells us if discovery is incomplete.
- Runtime discovery does not throw on repeated calls.
- Known items like chairs, tables, lamps, rugs, and toilets appear.

Status:

- Partially implemented as a player-useful suggestion slice instead of more raw debug output.
- Added `/housingadvisor suggest <category>` with store/craft availability.
- Store reading follows OpenNutriView's active-store scan.
- Craft hints follow Eco core `CraftingComponent.RecipesForItem(...)`.
- Per-player store authorization still needs confirmed `AccessType` namespace in `Mods/UserCode`.

## V0.6: Economy Viewer Spreadsheet Probe

Purpose: stop relying on chat for economy-style browsing and prepare a spreadsheet-like viewer.

Target experience:

```text
Eco Housing Advisor > Economy

Columns:
Item | Category | Base | Type limit | Duplicate mult | Skill | Best price | Stock | Seller
```

Preferred implementation order:

1. Try an in-game tab/window if Eco exposes a stable UI/tab API.
2. If in-game tabs are too risky, expose a small local web page from the mod.
3. If serving a page from the mod is blocked, export a CSV/JSON snapshot for a separate viewer.

Tasks:

- Investigate whether Eco 0.13 can add a custom in-game tab/window from a server mod.
- Reuse existing furniture cache for table rows.
- Add sortable/filterable columns in the chosen UI:
  - item name;
  - category;
  - base value;
  - type limit;
  - duplicate multiplier;
  - recipe/skill hint when available;
  - economy fields later.
- Keep economy columns empty or labeled "not read yet" until store reading is implemented.
- Add `/housingadvisor economy` as the command that opens the viewer or returns its URL/export path.
- Do not build full price optimization yet.

Acceptance:

- A tester can browse furniture data outside chat.
- The table is readable and filterable enough to compare items.
- If no in-game UI hook is available, the fallback page/export path is documented and usable.

## V0.7: Room Context Probe

Purpose: detect enough player/room context to start recommendations.

Tasks:

- Add `/housingadvisor room`.
- Detect the player's current room if Eco exposes it safely.
- If room detection is not reliable, support manual room category:
  - `/housingadvisor room Bedroom`
  - `/housingadvisor room Kitchen`
- Read placed world objects in the room if possible.
- Print:
  - detected/manual room category;
  - current furniture count seen by the advisor;
  - matching candidate furniture categories.
- Document any runtime API uncertainty.

Acceptance:

- Tester standing in a room can run `/housingadvisor room`.
- If auto-detect fails, manual mode still works.
- No recommendation math yet beyond matching categories.

## V0.8: First Recommendation Slice

Purpose: recommend useful furniture by base housing value and duplicate type.

Tasks:

- Add a domain `RoomSnapshot` fake model.
- Add a domain `RecommendationEngine`.
- Use:
  - furniture category;
  - base value;
  - type limit;
  - duplicate multiplier;
  - existing item/type counts when available.
- Output top 5 suggestions:
  - item/group name;
  - base value;
  - duplicate multiplier;
  - simple reason.
- Add fake tests:
  - no duplicate preferred;
  - duplicate penalty lowers rank;
  - wrong category excluded;
  - zero value excluded.

Acceptance:

- `/housingadvisor room` returns a short "Best additions" list.
- The ranking is simple but understandable.
- It does not claim exact real XP yet.

## V0.9: Better Real Gain Approximation

Purpose: move from base value to useful estimated gain.

Tasks:

- Implement duplicate diminishing return calculation in domain.
- Apply type limit grouping.
- Show estimated gain separately from base value.
- Add explanation text:
  - "first item of this type";
  - "duplicate reduced by multiplier";
  - "already near low gain".
- Add tests around duplicate counts.

Acceptance:

- Recommending a second chair shows lower gain than the first.
- Output remains compact.

## V0.10: Room Material Tier Awareness

Purpose: avoid recommending high-value items when room material tier caps them badly.

Tasks:

- Investigate runtime access for room material tier and housing config caps.
- Add manual fallback:
  - `/housingadvisor room Bedroom tier 2`
- Add domain tier cap model.
- Apply soft/hard cap if enough Eco data is confirmed.
- If uncertain, label output as estimate.

Acceptance:

- User can specify tier manually.
- Recommendations explain when gain is capped by room material quality.

## V0.11: Crafting Hints

Purpose: give food-advisor-like usefulness without full economy optimization.

Tasks:

- Safely read `CraftingComponent.RecipesForItem(...)`.
- Show first required skill/table if available.
- Output:
  - "Crafted with Carpentry";
  - "Recipe unknown";
  - "Runtime recipe read failed" in debug only.
- Do not calculate ingredient costs yet.

Acceptance:

- Recommended items include a simple craft hint when available.
- Missing recipe info does not break recommendations.

## V1.0: Playable Room Advisor

V1 command set:

```text
/housingadvisor help
/housingadvisor search chair
/housingadvisor category Seating
/housingadvisor config
/housingadvisor economy
/housingadvisor room
/housingadvisor room Bedroom
/housingadvisor room Bedroom tier 2
/housingadvisor hadebug
/housingadvisor harefresh
```

V1 output shape:

```text
Eco Housing Advisor - Bedroom, Tier 2
Current furniture seen: 7

Best additions:
1. Hewn Dresser
   estimated +2.4, base 3, type Storage
   reason: first useful storage item

2. Torch Stand
   estimated +1.8, base 2, type Light
   reason: lighting support

More:
/housingadvisor room Bedroom tier 2 page 2
```

V1 acceptance:

- Works on a live Eco server without startup errors.
- Responds fast after first cache build.
- Has at least one non-chat UI surface, preferably config/tooltip or a table viewer.
- Provides a spreadsheet-like furniture/economy viewer, either in-game or via fallback page/export.
- Recommends a short list for a room.
- Explains why each item is suggested.
- Handles unknown runtime APIs gracefully.
- Fake domain tests cover grouping, filtering, duplicate gain, and tier cap logic.
- README documents known limits.

## After V1

V1.1:

- Better localized item names if safe API is confirmed.
- Better room auto-detection.
- More exact Eco housing formula comparison.

V1.2:

- Storage/player-owned item hints.
- "Already have this" suggestions.

V1.3:

- Store/economy read-only price hints.
- Best XP per credit.
- Promote the economy viewer from furniture table to real price/stock table.

V2:

- Whole-house advisor.
- Move furniture between rooms.
- Global shopping/crafting list.

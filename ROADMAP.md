# Eco Housing Advisor Roadmap

## Current Pre-V1 State

Eco Housing Advisor is now close to production use as a tooltip-first Eco housing advisor.

Implemented:

- housing item tooltip shows maximum useful property gain and best placement;
- `Residency Property Value` tooltip shows compact best additions;
- suggestions use live property rooms instead of current-player-room guesses;
- unavailable furniture is hidden unless buyable, owned, or craftable by a known/no-skill path;
- duplicate type penalties, support caps, room tier caps, and bathroom/outdoor-style property caps are applied;
- room detail foldouts use Eco room value descriptions when readable;
- chat commands are admin-only diagnostics;
- OpenNutriView-style tooltip integration is the preferred UI surface.

## V1 Goal

V1 should be comfortable on a live server:

- no player-facing chat spam;
- no expensive full-world scan on every hover outside controlled caches;
- short, useful tooltip copy;
- Eco-native links/foldouts wherever possible;
- clear docs for installation, validation, and limitations;
- admin diagnostics available but hidden from normal players.

## Remaining V1 Work

- Confirm room detail foldouts with multiple same-type rooms.
- Test multiple residences, rentals, roommates, and shared deeds.
- Test room tier edge cases and hard/soft cap behavior with real placements.
- Validate utility blockers:
  - electricity;
  - water;
  - fuel;
  - chimney/pollution;
  - mechanical power if relevant.
- Improve fallback messages when Eco cannot expose a specific room object.
- Add release/version notes once the user confirms final in-game behavior.

## Later Work

- Economy optimization:
  - best XP per currency;
  - budget-limited shopping list;
  - equivalent item variants;
  - multi-shop shopping plan.
- Player configuration:
  - max suggestions;
  - hide categories;
  - preferred currency;
  - show/hide diagnostics.
- Possible richer UI:
  - economy viewer page or tab if Eco exposes a safe extension point;
  - table-like view for buy/craft/owned suggestions.

## Known Technical Boundaries

- Domain logic must stay independent from Eco runtime types.
- Runtime readers can use Eco APIs and reflection, but must be defensive.
- Tooltips may use `LocString`, `UILink`, and `TextLoc` directly.
- Admin chat commands stay plain text.
- The mod should not spawn fake homes, fake players, or fake residences for scoring.

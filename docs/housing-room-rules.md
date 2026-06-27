# Eco Housing Room Rules

Source checked on the local Eco server:

`C:\Program Files (x86)\Steam\steamapps\common\Eco Server\Mods\__core__\Systems\HousingValues.cs`

Eco configures housing categories in `HousingConfig.SetRoomCategories(...)`. These categories are not all equivalent. Some categories define a room type, while others only support a room.

## Extraction Method

Run:

```powershell
.\tools\extract-housing-room-rules.ps1
```

The extraction reads each `new RoomCategory()` line and derives useful placement with this rule:

- a category is useful in itself when `CanBeRoomCategory` is not false and `NegatesValue` is not true;
- a category is useful in every room whose `SupportingRoomCategoryNames` contains that category;
- a category with `SupportForAnyRoomType = true` is useful in every non-industrial room type.

## Extracted Useful Placement

| Category | Can define room | Useful in | Supports |
| --- | --- | --- | --- |
| Living Room | Yes | Living Room, Bedroom | Seating, Cultural |
| Bedroom | Yes | Bedroom | Living Room, Seating |
| Kitchen | Yes | Kitchen | Seating |
| Bathroom | Yes | Bathroom | Seating |
| Outdoor | Yes | Outdoor | Seating, Cultural |
| Cultural | Yes | Cultural, Living Room, Outdoor | Seating |
| Industrial | Negative | Avoid on residence property | |
| Seating | No | Living Room, Bedroom, Kitchen, Bathroom, Outdoor, Cultural | |
| Decoration | No | Living Room, Bedroom, Kitchen, Bathroom, Outdoor, Cultural | any room type |
| Lighting | No | Living Room, Bedroom, Kitchen, Bathroom, Outdoor, Cultural | any room type |

## Support Rules Found In Eco

Eco's vanilla config currently defines these support relationships:

- `Living Room` supports `Seating` and `Cultural`, with support capped to 25% of primary room value.
- `Bedroom` supports `Living Room` and `Seating`.
- `Kitchen` supports `Seating`.
- `Bathroom` supports `Seating`, and is capped to 33% of the rest of the property.
- `Outdoor` supports `Seating` and `Cultural`, is capped to 100% of the rest of the property, and is not auto-chosen as a room category.
- `Cultural` supports `Seating`, caps support to 20% of primary value, and allows `Outdoor` support up to 100%.
- `Seating` is not a room category and caps support to 30% of primary value.
- `Decoration` is not a room category, supports any room type, and caps support to 50% of primary value.
- `Lighting` is not a room category, supports any room type, and caps support to 50% of primary value.
- `Industrial` negates housing value.

## How The Mod Uses This In V0.6

The domain layer now maps a furniture category to useful room placements:

- `Seating` suggestions say they are useful in Living Room, Bedroom, Kitchen, Bathroom, Outdoor, and Cultural.
- `Decoration` and `Lighting` suggestions say they are useful in the normal residence room types.
- `Living Room` suggestions say they are useful in Living Room and Bedroom, because Eco's `Bedroom` room category supports `Living Room`.
- `Cultural` suggestions say they are useful in Cultural, Living Room, and Outdoor.
- Primary categories such as `Bedroom`, `Kitchen`, and `Bathroom` point to their matching room when no other room supports them.
- `Industrial` is marked as something to avoid on residence property.

This is still not the full room optimizer. It only prevents misleading output like "put a chair in Seating".

## Runtime API Uncertainty

The rules above are confirmed from Eco's vanilla C# config on this server. The mod currently keeps a small domain copy of those relationships because a stable `Mods/UserCode` API for reading every `RoomCategory` property at runtime has not yet been confirmed.

Before V1, investigate whether `HousingConfig` exposes the configured room categories safely from the mod runtime. If yes, replace the copied rule table with runtime discovery and keep this file as documentation/tests.

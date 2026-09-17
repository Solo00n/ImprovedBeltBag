# Changelog

## 1.4.0
- Belt bags **found on a moon** are now managed too. Mods that turn store items into scrap
  (Remnants) build them through LethalLib, which clones the Item and gives the clone its own
  prefab — so a found bag was a different Item than the one the shop sells, slipped past the
  vanilla-bag check added in 1.2.0 and ran on bare vanilla rules: no categories, no capacity,
  no weight. A clone keeps the id and name it was copied from, which is what identifies it now.
## 1.3.0
- Emptying the bag now takes a **double tap** of right-interact (E) instead of a single press.
  The key sits next to the ones you use constantly and the action throws away everything inside,
  so one press only arms it and shows a tip; a second press within `Empty Bag Double Tap Window`
  (0.4s by default, configurable) actually empties it. Holding the key never empties the bag.

## 1.2.0
- Compatibility: the mod now only manages the **vanilla** belt bag. Mods that add their own belt
  bag do it by cloning the vanilla prefab, so their bag carries the same `BeltBagItem` component
  and every patch here used to apply to it as well — silently rejecting its items, double-counting
  its weight and resizing its slot grid. The vanilla bag is now identified once at startup and
  everything else is left to the mod that owns it.

## 1.1.1
- Fix: the Easter Egg category now matches the actual "EasterEgg" collectible by name (was mistakenly matching the Kiwi egg).

## 1.1.0
- Added a separate `Easter Egg` category (off by default) so the Kiwi egg (laid by the
  Giant Kiwi) has its own Allow + Max, apart from generic scrap.

## 1.0.0
- Initial release.
- Configurable belt bags with **host-authoritative** networking (host validates every add
  server-side; a lone modded client gets no advantage).
- **Per-type item categories**, each with Allow + Max Amount: Tools, Shotgun, Knife, Signs,
  One-Handed Scrap, Two-Handed Scrap, Deny, plus per-item overrides. Default: equipment on,
  generic metal loot (scrap) off.
- **Capacity** and **grab range** (both host-enforced), HUD tips, empty-bag action.
- **Weight**: the bag weighs you down by its contents (configurable).
- **Resizable slot grid**: the inventory UI shows the configured number of slots, centred
  with the original spacing.
- Networking + item-category system adapted from BagConfig (MIT) by The Matty — see
  THIRD_PARTY_LICENSES.md.

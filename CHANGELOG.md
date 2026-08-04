# Changelog

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

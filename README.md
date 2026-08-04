# ImprovedBeltBag

**Language / Язык:** [English](#improvedbeltbag) · [Русский](#russian)

A BepInEx mod for Lethal Company that makes **Belt Bags** configurable — decide exactly
which item types they accept, cap each type, add weight, and resize the slot grid.

- **Per-type item categories**, each with its own `Allow` + `Max Amount`:
  `Tools`, `Shotgun`, `Knife`, `Signs`, `One Handed Scrap`, `Two Handed Scrap`, `Deny`,
  plus per-item overrides. So you can, say, let the bag carry tools/shotguns/knives but
  **not** generic metal loot — and limit how many one-handed vs two-handed junk items fit.
- **Capacity** and **grab range** (host-enforced).
- **Weight**: the bag weighs you down by its contents (vanilla bags are weightless).
- **Resizable slot grid**: the inventory UI shows the configured number of slots, centred
  with the original spacing.
- HUD tips + an "empty bag" action (right-interact while holding it).

## Multiplayer (host-authoritative)

Bigger/permissive bags are a competitive advantage, so this is **host-authoritative**: the
tweaks stay off on a client until the host confirms it has the mod, and every add is
validated **server-side** against the host's config. A lone modded client in a vanilla
lobby gets no advantage, and clients can't exceed the host's limits. Vanilla clients can
still join.

## Install
1. Install **BepInEx 5** (BepInExPack for Lethal Company).
2. Put `Iron.ImprovedBeltBag.dll` in `Lethal Company/BepInEx/plugins/`.
3. Launch once to generate `BepInEx/config/Iron.ImprovedBeltBag.cfg`.

## Config (`Iron.ImprovedBeltBag.cfg`)
| Section | Key | Default | Meaning |
|---------|-----|---------|---------|
| General | `Enabled` | `true` | Master switch. |
| Slots | `Capacity` | `15` | How many items fit. |
| Slots | `Resize Inventory UI` | `true` | Rebuild the slot grid to match Capacity. |
| Slots | `Slots Per Row` | `5` | Columns in the rebuilt grid. |
| Items | `Per-Item Overrides` | `Body: Deny, Apparatus: Deny` | `ItemName: Category` map. |
| Category.\* | `Allow` / `Max Amount` | — | Per-category toggle + cap. Tools/Shotgun/Knife/Signs on; scrap off. |
| Weight | `Enabled` / `Multiplier` | `true` / `1.0` | Contents add weight. |
| Misc | `Grab Range` / `Tooltips` / `Empty Bag Action` | `4` / on / on | |
| Host | `Enforce Capacity` / `Restrictions` / `Range` | on | Server-side checks. |

## Credits
The networking model and item-category system are adapted from **BagConfig** by
**The Matty (mattymatty)** (MIT) — <https://github.com/mattymatty97/LTC_BagConfig>. The
weight feature and resizable slot grid are original. See `THIRD_PARTY_LICENSES.md`.

## Build
`dotnet build -c Release` → `bin/Release/Iron.ImprovedBeltBag.dll`. Uses the publicized
`LethalCompany.GameLibs.Steam` NuGet package (match the version to your build).

---

<a id="russian"></a>

# ImprovedBeltBag — Русское описание

**[⤴ English](#improvedbeltbag)**

Мод на BepInEx, делающий **belt bag (поясную сумку)** настраиваемой — ты сам решаешь, какие
типы предметов она принимает, ставишь лимиты, вес и меняешь число слотов.

- **Категории по типам**, у каждой свои `Allow` + `Max Amount`:
  `Tools` (инструменты), `Shotgun`, `Knife`, `Signs` (знаки), `One Handed Scrap`,
  `Two Handed Scrap`, `Deny`, плюс пер-предметные оверрайды. Можно, например, разрешить
  инструменты/дробовик/ножи, но **запретить металлолом** — и отдельно ограничить, сколько
  влезает одноручного и двуручного хлама.
- **Вместимость** и **дальность захвата** (энфорсятся хостом).
- **Вес**: сумка тяжелеет от содержимого (ванильные — невесомые).
- **Настраиваемая сетка слотов**: инвентарь показывает заданное число слотов, по центру и с
  оригинальными отступами.
- HUD-подсказки + «высыпать всё» (правый interact держа сумку).

## Мультиплеер (host-authoritative)
Более вместительная сумка — это преимущество, поэтому мод **управляется хостом**: твики
выключены на клиенте, пока хост не подтвердит наличие мода, а каждое добавление проверяется
**на сервере** по конфигу хоста. Одиночный клиент преимущества не получает и не может
превысить лимиты хоста. Ваниль-клиенты при этом могут заходить.

## Установка
1. Поставь **BepInEx 5** (BepInExPack for Lethal Company).
2. Положи `Iron.ImprovedBeltBag.dll` в `Lethal Company/BepInEx/plugins/`.
3. Запусти раз — создастся `BepInEx/config/Iron.ImprovedBeltBag.cfg`.

## Авторство
Сетевая модель и система категорий адаптированы из **BagConfig** от **The Matty
(mattymatty)** (MIT) — <https://github.com/mattymatty97/LTC_BagConfig>. Вес и настраиваемая
сетка слотов — оригинальные. См. `THIRD_PARTY_LICENSES.md`.

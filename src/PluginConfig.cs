using System.Collections.Generic;
using System.Collections.Specialized;
using BepInEx.Configuration;

namespace ImprovedBeltBag
{
    /// <summary>
    /// All config for the mod.
    ///
    /// The item-category system (Tools / One-Handed Scrap / Two-Handed Scrap / Deny, each with
    /// Allow + Max, plus per-item overrides) is adapted from mattymatty's BagConfig (MIT) — see
    /// THIRD_PARTY_LICENSES.md. Weight and Slots are new.
    ///
    /// These values are the HOST's rules. In multiplayer the host is authoritative: the actual
    /// add is validated server-side against the host's config, so a client cannot exceed them.
    /// A client's own config only drives that client's local prediction / UI feedback.
    /// </summary>
    internal static class PluginConfig
    {
        internal const string ToolsCategory = "Tools";
        internal const string ShotgunCategory = "Shotgun";
        internal const string KnifeCategory = "Knife";
        internal const string SignsCategory = "Signs";
        internal const string EasterEggCategory = "Easter Egg";
        internal const string OneHandedScrapCategory = "One Handed Scrap";
        internal const string TwoHandedScrapCategory = "Two Handed Scrap";
        internal const string DenyCategory = "Deny";

        internal static void Init(ConfigFile cfg)
        {
            // ---- General ----
            Enabled = cfg.Bind("General", "Enabled", true,
                "Master switch for the whole mod.");

            // ---- Capacity / slots ----
            Capacity = cfg.Bind("Slots", "Capacity", 15,
                new ConfigDescription("How many items the bag can hold.", new AcceptableValueRange<int>(1, 100)));
            ResizeSlotUI = cfg.Bind("Slots", "Resize Inventory UI", true,
                "Rebuild the belt bag inventory grid so the number of visible slot squares matches Capacity.");
            SlotsPerRow = cfg.Bind("Slots", "Slots Per Row", 5,
                new ConfigDescription("How many slot squares per row when the UI is rebuilt.",
                    new AcceptableValueRange<int>(1, 20)));

            // ---- Item categories (what can be stored) ----
            ItemCategories = cfg.Bind("Items", "Per-Item Overrides", "Body: Deny, Apparatus: Deny",
                "Comma-separated 'ItemName: CategoryName' overrides, e.g. 'Zap gun: Deny, Gold bar: Scrap'. " +
                "Any category name you use here is auto-created below with Allow/Max settings.");

            // Equipment types each get their OWN toggle, separate from generic 'Scrap' (metal loot).
            CategoryConfigs[ToolsCategory] = new CategoryConfig(cfg, ToolsCategory, allowDefault: true,
                "Non-scrap tools/equipment: flashlight, walkie-talkie, zap gun, extension ladder, shovel, etc.");
            CategoryConfigs[ShotgunCategory] = new CategoryConfig(cfg, ShotgunCategory, allowDefault: true,
                "The Shotgun.");
            CategoryConfigs[KnifeCategory] = new CategoryConfig(cfg, KnifeCategory, allowDefault: true,
                "The Kitchen knife.");
            CategoryConfigs[SignsCategory] = new CategoryConfig(cfg, SignsCategory, allowDefault: true,
                "Stop sign / Yield sign.");
            CategoryConfigs[EasterEggCategory] = new CategoryConfig(cfg, EasterEggCategory, allowDefault: false,
                "The Kiwi egg (the \"Easter egg\" laid by the Giant Kiwi). OFF by default.");
            CategoryConfigs[OneHandedScrapCategory] = new CategoryConfig(cfg, OneHandedScrapCategory, allowDefault: false,
                "Generic ONE-handed sellable scrap / metal loot (not knife/signs). OFF by default. " +
                "Use 'Max Amount' to cap how many one-handed junk items fit.");
            CategoryConfigs[TwoHandedScrapCategory] = new CategoryConfig(cfg, TwoHandedScrapCategory, allowDefault: false,
                "Generic TWO-handed sellable scrap / large metal loot (not shotgun). OFF by default. " +
                "Use 'Max Amount' to cap how many two-handed junk items fit.");
            CategoryConfigs[DenyCategory] = new StaticCategoryConfig(DenyCategory, allow: false, limit: 0);

            RebuildAssociations();
            ItemCategories.SettingChanged += (_, _) => RebuildAssociations();

            // ---- Weight (idea from WeightedBeltBag) ----
            WeightEnabled = cfg.Bind("Weight", "Enabled", true,
                "Make the bag weigh you down by the weight of its contents (vanilla bags are weightless).");
            WeightMultiplier = cfg.Bind("Weight", "Multiplier", 1.0f,
                new ConfigDescription("Fraction of each stored item's weight that is applied while you carry the bag.",
                    new AcceptableValueRange<float>(0f, 2f)));

            // ---- Misc ----
            GrabRange = cfg.Bind("Misc", "Grab Range", 4f,
                new ConfigDescription("Max range to grab items into the bag.", new AcceptableValueRange<float>(0f, 20f)));
            Tooltip = cfg.Bind("Misc", "Tooltips", true,
                "Show a HUD tip when an item can't be stored (full / not allowed / limit reached).");
            DropAll = cfg.Bind("Misc", "Empty Bag Action", true,
                "Right-interact while holding the bag empties it onto the ground.");

            // ---- Host enforcement (server-side, authoritative) ----
            EnforceCapacity = cfg.Bind("Host", "Enforce Capacity", true,
                "Server-side capacity check (prevents clients exceeding the host's Capacity).");
            EnforceCategory = cfg.Bind("Host", "Enforce Restrictions", true,
                "Server-side item-restriction check (prevents clients bagging items the host disallows).");
            EnforceRange = cfg.Bind("Host", "Enforce Range", true,
                "Server-side grab-range check.");

            // remove stale entries
            var orphans = (Dictionary<ConfigDefinition, string>)typeof(ConfigFile)
                .GetProperty("OrphanedEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(cfg);
            orphans?.Clear();
            cfg.Save();
        }

        // ---- General ----
        internal static ConfigEntry<bool> Enabled;

        // ---- Slots ----
        internal static ConfigEntry<int> Capacity;
        internal static ConfigEntry<bool> ResizeSlotUI;
        internal static ConfigEntry<int> SlotsPerRow;

        // ---- Items ----
        internal static ConfigEntry<string> ItemCategories;
        internal static StringDictionary Associations = new StringDictionary();
        internal static readonly Dictionary<string, ICategoryConfig> CategoryConfigs = new Dictionary<string, ICategoryConfig>();

        // ---- Weight ----
        internal static ConfigEntry<bool> WeightEnabled;
        internal static ConfigEntry<float> WeightMultiplier;

        // ---- Misc ----
        internal static ConfigEntry<float> GrabRange;
        internal static ConfigEntry<bool> Tooltip;
        internal static ConfigEntry<bool> DropAll;

        // ---- Host ----
        internal static ConfigEntry<bool> EnforceCapacity;
        internal static ConfigEntry<bool> EnforceCategory;
        internal static ConfigEntry<bool> EnforceRange;

        private static void RebuildAssociations()
        {
            var dict = new StringDictionary();
            foreach (var pair in ItemCategories.Value.Split(','))
            {
                var trimmed = pair.Trim();
                if (trimmed.Length == 0) continue;
                var parts = trimmed.Split(':');
                if (parts.Length != 2)
                {
                    Plugin.Log.LogError($"Per-Item Overrides: malformed entry '{trimmed}'");
                    continue;
                }
                dict[parts[0].Trim()] = parts[1].Trim();
            }
            Associations = dict;

            // auto-create any referenced category
            foreach (string category in dict.Values)
            {
                if (!CategoryConfigs.ContainsKey(category))
                    CategoryConfigs[category] = new CategoryConfig(Plugin.Instance.Config, category, allowDefault: true, "Custom category.");
            }
        }

        // ---- category config abstraction ----

        internal interface ICategoryConfig
        {
            string CategoryName { get; }
            bool Allow { get; }
            int Limit { get; }
        }

        internal sealed class StaticCategoryConfig : ICategoryConfig
        {
            public string CategoryName { get; }
            public bool Allow { get; }
            public int Limit { get; }
            public StaticCategoryConfig(string name, bool allow, int limit)
            {
                CategoryName = name; Allow = allow; Limit = limit;
            }
        }

        internal sealed class CategoryConfig : ICategoryConfig
        {
            public string CategoryName { get; }
            public bool Allow => _allow.Value;
            public int Limit => _limit.Value;
            private readonly ConfigEntry<bool> _allow;
            private readonly ConfigEntry<int> _limit;

            public CategoryConfig(ConfigFile cfg, string name, bool allowDefault, string desc)
            {
                CategoryName = name;
                _allow = cfg.Bind($"Category.{name}", "Allow", allowDefault, $"Allow storing {name}. {desc}");
                _limit = cfg.Bind($"Category.{name}", "Max Amount", -1,
                    new ConfigDescription($"Max number of {name} at once (-1 = only limited by Capacity).",
                        new AcceptableValueRange<int>(-1, 100)));
            }
        }

        /// <summary>Which category a grabbable falls into (per-item override, else scrap/two-handed heuristic).</summary>
        internal static ICategoryConfig CategoryOf(GrabbableObject grabbable)
        {
            string name = grabbable.itemProperties.itemName;
            if (name != null && Associations.ContainsKey(name) && CategoryConfigs.TryGetValue(Associations[name], out var overridden))
                return overridden;

            if (ItemClassifier.IsShotgun(grabbable)) return CategoryConfigs[ShotgunCategory];
            if (ItemClassifier.IsKnife(grabbable)) return CategoryConfigs[KnifeCategory];
            if (ItemClassifier.IsSign(grabbable)) return CategoryConfigs[SignsCategory];
            if (ItemClassifier.IsEasterEgg(grabbable)) return CategoryConfigs[EasterEggCategory];
            if (!grabbable.itemProperties.isScrap) return CategoryConfigs[ToolsCategory];
            if (!grabbable.itemProperties.twoHanded) return CategoryConfigs[OneHandedScrapCategory];
            return CategoryConfigs[TwoHandedScrapCategory];
        }
    }
}

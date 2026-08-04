using System;

namespace ImprovedBeltBag
{
    /// <summary>
    /// Recognises specific "equipment" items so they can have their own belt-bag category
    /// (separate from generic sellable scrap / metal loot). Matches by runtime class and by
    /// itemName for robustness.
    /// </summary>
    internal static class ItemClassifier
    {
        public static bool IsShotgun(GrabbableObject g) => HasClass(g, "ShotgunItem") || NameIs(g, "shotgun");

        public static bool IsKnife(GrabbableObject g) => HasClass(g, "KnifeItem") || NameContains(g, "knife");

        public static bool IsSign(GrabbableObject g)
        {
            string n = Name(g);
            if (n == "stop sign" || n == "yield sign") return true;
            // Stop/Yield signs reuse the Shovel class and are scrap; the plain Shovel is not scrap.
            return HasClass(g, "Shovel") && g.itemProperties != null && g.itemProperties.isScrap;
        }

        private static string Name(GrabbableObject g) =>
            g.itemProperties != null && g.itemProperties.itemName != null
                ? g.itemProperties.itemName.Trim().ToLowerInvariant()
                : "";

        private static bool NameIs(GrabbableObject g, string s) => Name(g) == s;
        private static bool NameContains(GrabbableObject g, string s) => Name(g).Contains(s);

        private static bool HasClass(GrabbableObject g, string simpleName)
        {
            for (Type t = g.GetType(); t != null && t != typeof(GrabbableObject); t = t.BaseType)
                if (t.Name == simpleName) return true;
            return false;
        }
    }
}

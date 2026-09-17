using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;

namespace ImprovedBeltBag.Utils
{
    /// <summary>
    /// Tells the game's own belt bag apart from custom belt bags added by other mods.
    ///
    /// Mods that add a new bag (ScrapSack, for one) do it by cloning the vanilla prefab at
    /// runtime, so the clone carries the very same <see cref="BeltBagItem"/> component. Harmony
    /// patches methods, not instances, so without this check every patch in this mod would also
    /// apply to those bags and fight the mod that owns them — silently rejecting their items,
    /// double-counting their weight and resizing their slot grid.
    ///
    /// The vanilla Item is recorded during <c>GameNetworkManager.Start</c>, while the
    /// NetworkManager's prefab list still holds only the game's own prefabs: mods append theirs
    /// after that method returns, so whatever bag is in the list at this point is the vanilla one.
    /// </summary>
    [HarmonyPatch]
    internal static class VanillaBeltBag
    {
        private const string VanillaItemName = "Belt bag";

        private static Item _vanillaItem;
        private static bool _captured;

        // One verdict per Item: the interact, weight and UI paths ask this constantly.
        private static readonly Dictionary<Item, bool> KnownItems = new Dictionary<Item, bool>();

        [HarmonyPrefix]
        [HarmonyPriority(int.MaxValue)] // must beat mods that register their bag here (ScrapSack uses 800)
        [HarmonyPatch(typeof(GameNetworkManager), "Start")]
        private static void CaptureBeforeModsRegister() => Capture();

        private static void Capture()
        {
            if (_captured) return;
            _captured = true;

            var prefabs = NetworkManager.Singleton?.NetworkConfig?.Prefabs?.Prefabs;
            if (prefabs != null)
            {
                Item firstItem = null, namedVanilla = null;
                int seen = 0;

                foreach (var entry in prefabs)
                {
                    var go = entry?.Prefab;
                    if (go == null) continue;

                    var bag = go.GetComponent<BeltBagItem>();
                    if (bag == null || bag.itemProperties == null) continue;

                    seen++;
                    var item = bag.itemProperties;
                    Plugin.Log.LogInfo($"[diag] belt bag prefab #{seen}: item=\"{item.itemName}\" " +
                                       $"itemId={item.itemId} hash={item.GetInstanceID()} prefab={go.name}");

                    if (firstItem == null) firstItem = item;
                    if (namedVanilla == null && item.itemName == VanillaItemName) namedVanilla = item;
                }

                // Normally only the game's own bag exists at this point. If a mod got in first we
                // still prefer the one carrying the vanilla name over blind "first wins".
                if (seen > 1)
                    Plugin.Log.LogWarning(
                        $"{seen} belt bag prefabs were already registered before this mod looked; " +
                        "another mod registers its bag very early. Falling back to the one named " +
                        $"\"{VanillaItemName}\".");

                _vanillaItem = namedVanilla ?? firstItem;
                if (_vanillaItem != null)
                {
                    Plugin.Log.LogInfo($"Vanilla belt bag identified as \"{_vanillaItem.itemName}\" " +
                                       $"(itemId={_vanillaItem.itemId} hash={_vanillaItem.GetInstanceID()}).");
                    return;
                }
            }

            Plugin.Log.LogWarning(
                "Could not identify the vanilla belt bag, so every belt bag will be managed. " +
                "If a mod that adds its own belt bag is installed, the two will conflict.");
        }

        /// <summary>
        /// True if this mod should manage the bag. If identification failed we fall back to
        /// managing everything, which is how the mod behaved before this check existed.
        /// </summary>
        internal static bool IsManaged(BeltBagItem bag)
        {
            if (bag == null) return false;
            if (_vanillaItem == null) return true;

            var props = bag.itemProperties;
            if (props == null) return false;
            if (ReferenceEquals(props, _vanillaItem)) return true;

            if (KnownItems.TryGetValue(props, out bool managed)) return managed;

            managed = IsCopyOfVanilla(props);
            KnownItems[props] = managed;
            if (managed)
                Plugin.Log.LogInfo(
                    $"\"{props.itemName}\" is a copy of the vanilla belt bag made by another mod; managing it too.");
            return managed;
        }

        /// <summary>
        /// True for the copies of the vanilla bag that appear when a mod turns store items into
        /// scrap to be found on a moon — Remnants is the common one.
        ///
        /// Those go through LethalLib, which clones the Item field by field and hands the clone a
        /// prefab of its own, so the bag lying on the floor is a different Item than the one the
        /// shop sells and a plain reference check leaves it running on bare vanilla rules. What a
        /// clone does keep is the id and the name it was copied from, and that pair is enough to
        /// tell it apart: a mod that adds a bag of its own gives it an id of its own.
        /// </summary>
        private static bool IsCopyOfVanilla(Item props)
            => props.itemId == _vanillaItem.itemId && props.itemName == _vanillaItem.itemName;
    }
}

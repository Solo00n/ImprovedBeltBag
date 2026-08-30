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
        private static Item _vanillaItem;
        private static bool _captured;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(GameNetworkManager), "Start")]
        private static void CaptureBeforeModsRegister() => Capture();

        private static void Capture()
        {
            if (_captured) return;
            _captured = true;

            var prefabs = NetworkManager.Singleton?.NetworkConfig?.Prefabs?.Prefabs;
            if (prefabs != null)
            {
                foreach (var entry in prefabs)
                {
                    var go = entry?.Prefab;
                    if (go == null) continue;

                    var bag = go.GetComponent<BeltBagItem>();
                    if (bag == null || bag.itemProperties == null) continue;

                    _vanillaItem = bag.itemProperties;
                    Plugin.Log.LogInfo($"Vanilla belt bag identified as \"{_vanillaItem.itemName}\".");
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
            return ReferenceEquals(bag.itemProperties, _vanillaItem);
        }
    }
}

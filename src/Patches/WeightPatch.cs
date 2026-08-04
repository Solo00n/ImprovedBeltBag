using System.Runtime.CompilerServices;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace ImprovedBeltBag.Patches
{
    /// <summary>
    /// Makes a belt bag weigh you down by the weight of its contents (vanilla bags are weightless
    /// — the idea comes from WeightedBeltBag).
    ///
    /// Implemented as a self-correcting reconcile driven by the bag's own LateUpdate: every frame
    /// we make sure exactly the right amount of extra weight is applied to whoever currently holds
    /// the bag, and cleaned up when the bag is dropped / deactivated. This avoids the fragile
    /// bookkeeping of hooking every grab/drop/add/remove path.
    ///
    /// Weight is a self-DISadvantage (heavier = slower), so it is gated on the host-present flag
    /// but not server-enforced.
    /// </summary>
    [HarmonyPatch]
    internal static class WeightPatch
    {
        private sealed class State { public PlayerControllerB Player; public float Amount; }

        private static readonly ConditionalWeakTable<BeltBagItem, State> Applied =
            new ConditionalWeakTable<BeltBagItem, State>();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeltBagItem), "LateUpdate")]
        private static void AfterLateUpdate(BeltBagItem __instance) => Reconcile(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeltBagItem), "OnDisable")]
        private static void AfterOnDisable(BeltBagItem __instance) => Remove(__instance);

        private static float DesiredWeight(BeltBagItem bag)
        {
            if (!BeltBagPatch.Enabled || !PluginConfig.Enabled.Value || !PluginConfig.WeightEnabled.Value)
                return 0f;
            if (bag.playerHeldBy == null || bag.objectsInBag == null)
                return 0f;

            float mult = PluginConfig.WeightMultiplier.Value;
            float sum = 0f;
            foreach (var item in bag.objectsInBag)
            {
                if (item == null || item.itemProperties == null) continue;
                sum += Mathf.Clamp(item.itemProperties.weight - 1f, 0f, 10f);
            }
            return sum * mult;
        }

        private static void Reconcile(BeltBagItem bag)
        {
            if (bag == null) return;
            var st = Applied.GetOrCreateValue(bag);

            var holder = (BeltBagPatch.Enabled && PluginConfig.Enabled.Value && PluginConfig.WeightEnabled.Value)
                ? bag.playerHeldBy : null;
            float desired = holder != null ? DesiredWeight(bag) : 0f;

            // Remove stale application (holder changed, weight changed, or now zero).
            if (st.Player != null && (st.Player != holder || !Mathf.Approximately(st.Amount, desired)))
            {
                st.Player.carryWeight = Mathf.Max(1f, st.Player.carryWeight - st.Amount);
                st.Player = null;
                st.Amount = 0f;
            }

            // Apply fresh.
            if (holder != null && desired > 0f && st.Player == null)
            {
                holder.carryWeight += desired;
                st.Player = holder;
                st.Amount = desired;
            }
        }

        private static void Remove(BeltBagItem bag)
        {
            if (bag == null) return;
            if (Applied.TryGetValue(bag, out var st) && st.Player != null)
            {
                st.Player.carryWeight = Mathf.Max(1f, st.Player.carryWeight - st.Amount);
                st.Player = null;
                st.Amount = 0f;
            }
        }
    }
}

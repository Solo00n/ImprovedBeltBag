using System;
using System.Collections;
using System.Runtime.CompilerServices;
using HarmonyLib;
using ImprovedBeltBag.Utils;
using Unity.Netcode;
using UnityEngine;

namespace ImprovedBeltBag.Patches
{
    /// <summary>
    /// Core belt-bag behaviour: category/capacity/range filtering with client-side prediction
    /// and host-authoritative server-side enforcement. Adapted from BagConfig (MIT).
    ///
    /// <see cref="Enabled"/> is the host-authoritative gate (see NamedMessages): all client
    /// behaviour here is inert until the host confirms it has the mod.
    /// </summary>
    [HarmonyPatch]
    internal static class BeltBagPatch
    {
        internal static bool Enabled;

        // The Maneater / baby cave dweller — a live AI that soft-locks if bagged (also blocked by vanilla).
        private const int ManeaterItemIdA = 123984;
        private const int ManeaterItemIdB = 819501;

        // ---------------------------------------------------------------- grab override

        // Vanilla BeltBagItem.ItemInteractLeftRight only grabs on LEFT and its base
        // (GrabbableObject.ItemInteractLeftRight) is an empty method, so a Prefix that returns
        // false cleanly replaces it with our own capacity/category-aware logic.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.ItemInteractLeftRight))]
        private static bool ItemInteractLeftRight_Prefix(BeltBagItem __instance, bool right)
        {
            if (!Enabled || !PluginConfig.Enabled.Value) return true; // vanilla

            try
            {
                if (right) TryDumpItems(__instance);
                else TryGrabItem(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"ItemInteractLeftRight_Prefix error: {e}");
            }
            return false; // skip vanilla (base is empty)
        }

        private static void TryGrabItem(BeltBagItem bag)
        {
            var player = bag.playerHeldBy;
            if (player == null || player.currentlyHeldObjectServer != bag) return;
            if (bag.tryingAddToBag) return;

            if (bag.objectsInBag.Count >= PluginConfig.Capacity.Value)
            {
                Tip("This bag is full!");
                return;
            }

            var cam = player.gameplayCamera.transform;
            if (!Physics.Raycast(cam.position, cam.forward, out var hit,
                    PluginConfig.GrabRange.Value, player.interactableObjectsMask))
                return;

            if (hit.collider.gameObject.layer == 8 || !hit.collider.CompareTag("PhysicsProp"))
                return;

            var target = hit.collider.GetComponent<GrabbableObject>();
            if (!CanBePutInBag(bag, target)) return;

            if (CheckBagFilters(bag, target, out bool limited, out bool disallowed))
            {
                bag.TryAddObjectToBag(target);
                return;
            }

            if (disallowed)
                Tip($"Cannot store {target.itemProperties.itemName} in the bag!");
            else if (limited)
                Tip($"Cannot store any more {PluginConfig.CategoryOf(target).CategoryName} in the bag!");
        }

        private static void TryDumpItems(BeltBagItem bag)
        {
            if (!PluginConfig.DropAll.Value) return;
            if (bag.playerHeldBy == null || bag.playerHeldBy.currentlyHeldObjectServer != bag) return;
            bag.StartCoroutine(EmptyBag(bag));
        }

        private static IEnumerator EmptyBag(BeltBagItem bag)
        {
            while (bag.objectsInBag.Count > 0)
            {
                bag.RemoveObjectFromBag(0);
                yield return new WaitForEndOfFrame();
            }
        }

        // ---------------------------------------------------------------- filters

        private static bool CanBePutInBag(BeltBagItem bag, GrabbableObject g)
        {
            return g != null && g != bag && !g.isHeld && !g.isHeldByEnemy
                   && g.itemProperties.itemId != ManeaterItemIdA
                   && g.itemProperties.itemId != ManeaterItemIdB;
        }

        /// <summary>True if the item is allowed right now. Sets <paramref name="limited"/> /
        /// <paramref name="disallowed"/> to explain a refusal.</summary>
        private static bool CheckBagFilters(BeltBagItem bag, GrabbableObject g, out bool limited, out bool disallowed)
        {
            limited = false;
            var cat = PluginConfig.CategoryOf(g);
            disallowed = !cat.Allow;
            if (disallowed) return false;

            int limit = cat.Limit;
            if (limit < 0) return true;          // only Capacity limits it
            if (limit == 0) { limited = true; return false; }

            if (CountInCategory(bag, cat) >= limit) { limited = true; return false; }
            return true;
        }

        private static void Tip(string message)
        {
            if (PluginConfig.Tooltip.Value && HUDManager.Instance != null)
                HUDManager.Instance.DisplayTip("Belt bag", message);
        }

        // ---------------------------------------------------------------- per-category counting

        private sealed class Counter { public int Count; }

        private static readonly ConditionalWeakTable<BeltBagItem, ConditionalWeakTable<PluginConfig.ICategoryConfig, Counter>>
            Memory = new ConditionalWeakTable<BeltBagItem, ConditionalWeakTable<PluginConfig.ICategoryConfig, Counter>>();

        private static int CountInCategory(BeltBagItem bag, PluginConfig.ICategoryConfig cat)
        {
            var inner = Memory.GetOrCreateValue(bag);
            return inner.TryGetValue(cat, out var c) ? c.Count : 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.PutObjectInBagLocalClient))]
        private static void OnPutInBag(BeltBagItem __instance, GrabbableObject gObject, ref bool __state)
        {
            if (__state) return; // was already in the bag (see prefix below)
            var inner = Memory.GetOrCreateValue(__instance);
            inner.GetOrCreateValue(PluginConfig.CategoryOf(gObject)).Count += 1;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.PutObjectInBagLocalClient))]
        private static void BeforePutInBag(BeltBagItem __instance, GrabbableObject gObject, ref bool __state)
        {
            __state = __instance.objectsInBag.Contains(gObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.RemoveFromBagLocalClient))]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.RemoveFromBagLocalClientNonElevatorParent))]
        private static void OnRemoveFromBag(BeltBagItem __instance, NetworkObjectReference objectRef)
        {
            var inner = Memory.GetOrCreateValue(__instance);
            if (objectRef.TryGet(out var no) && no.TryGetComponent<GrabbableObject>(out var g))
            {
                var c = inner.GetOrCreateValue(PluginConfig.CategoryOf(g));
                if (c.Count > 0) c.Count -= 1;
            }
            if (__instance.objectsInBag.Count == 0) inner.Clear();
        }

        // ---------------------------------------------------------------- server-side enforcement

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.TryAddObjectToBagServerRpc))]
        private static bool EnforceOnServer(BeltBagItem __instance, NetworkObjectReference netObjectRef, int playerWhoAdded)
        {
            if (!Enabled || !PluginConfig.Enabled.Value) return true;
            if (!__instance.IsRpcServerStage()) return true; // only the server's execution
            if (!netObjectRef.TryGet(out var no)) return true;

            var g = no.GetComponent<GrabbableObject>();

            if (!CanBePutInBag(__instance, g))
            {
                __instance.CancelAddObjectToBagClientRpc(playerWhoAdded);
                return false;
            }
            if (PluginConfig.EnforceCapacity.Value && __instance.objectsInBag.Count >= PluginConfig.Capacity.Value)
            {
                __instance.CancelAddObjectToBagClientRpc(playerWhoAdded);
                return false;
            }
            if (PluginConfig.EnforceCategory.Value && !CheckBagFilters(__instance, g, out _, out _))
            {
                __instance.CancelAddObjectToBagClientRpc(playerWhoAdded);
                return false;
            }
            if (PluginConfig.EnforceRange.Value)
            {
                var camPos = __instance.playerHeldBy != null
                    ? __instance.playerHeldBy.gameplayCamera.transform.position
                    : Vector3.positiveInfinity;
                if (Vector3.Distance(g.transform.position, camPos) > PluginConfig.GrabRange.Value + 1f)
                {
                    __instance.CancelAddObjectToBagClientRpc(playerWhoAdded);
                    return false;
                }
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BeltBagItem), nameof(BeltBagItem.CancelAddObjectToBagClientRpc))]
        private static void OnServerRefusal(int playerWhoAdded)
        {
            if (StartOfRound.Instance == null) return;
            if (StartOfRound.Instance.allPlayerScripts[playerWhoAdded] != GameNetworkManager.Instance.localPlayerController)
                return;
            Tip("The host refused that item.");
        }
    }
}

using System.Collections.Generic;
using ImprovedBeltBag.Patches;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImprovedBeltBag.Networking
{
    /// <summary>
    /// Host-authoritative handshake, done with raw Netcode named messages (no library dependency).
    /// The tweaks stay OFF on a client until the host sends <c>HostPresent</c>, so a lone modded
    /// client in a vanilla lobby gets no change. Pattern adapted from BagConfig (MIT).
    /// </summary>
    internal static class NamedMessages
    {
        private static readonly string Base = "Iron.ImprovedBeltBag";
        private static readonly string HostPresentMsg = Base + "|HostPresent";
        private static readonly string FixBagsMsg = Base + "|FixBags";

        internal static void Register()
        {
            var mm = NetworkManager.Singleton?.CustomMessagingManager;
            if (mm == null) return;
            mm.RegisterNamedMessageHandler(HostPresentMsg, OnHostPresent);
            mm.RegisterNamedMessageHandler(FixBagsMsg, OnFixBags);
        }

        internal static void Unregister()
        {
            var mm = NetworkManager.Singleton?.CustomMessagingManager;
            if (mm == null) return;
            mm.UnregisterNamedMessageHandler(HostPresentMsg);
            mm.UnregisterNamedMessageHandler(FixBagsMsg);
        }

        internal static void SendHostPresent(IReadOnlyList<ulong> targets = null)
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || nm.CustomMessagingManager == null) return;
            var writer = new FastBufferWriter(0, Allocator.Temp);
            if (targets == null)
                nm.CustomMessagingManager.SendNamedMessageToAll(HostPresentMsg, writer);
            else
                nm.CustomMessagingManager.SendNamedMessage(HostPresentMsg, targets, writer);
        }

        private static void OnHostPresent(ulong senderId, FastBufferReader reader)
        {
            if (senderId != NetworkManager.ServerClientId) return;
            BeltBagPatch.Enabled = true;
            if (PluginConfig.Enabled != null && Plugin.Log != null)
                Plugin.Log.LogInfo("Host has ImprovedBeltBag — bag tweaks enabled (client).");
        }

        /// <summary>Host → clients: reset any bags stuck in a mid-operation state.</summary>
        internal static void SendFixBags()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsServer || nm.CustomMessagingManager == null) return;
            var writer = new FastBufferWriter(0, Allocator.Temp);
            nm.CustomMessagingManager.SendNamedMessageToAll(FixBagsMsg, writer);
        }

        private static void OnFixBags(ulong senderId, FastBufferReader reader)
        {
            if (senderId != NetworkManager.ServerClientId || StartOfRound.Instance == null) return;
            foreach (var bag in Object.FindObjectsByType<BeltBagItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bag.tryingAddToBag = false;
                bag.tryingCheckBag = false;
            }
        }
    }
}

using HarmonyLib;
using ImprovedBeltBag.Networking;
using Unity.Netcode;

namespace ImprovedBeltBag.Patches
{
    /// <summary>Wires the host-authoritative handshake into the game's network lifecycle.</summary>
    [HarmonyPatch]
    internal static class NetworkPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.Initialize))]
        private static void AfterNetworkInitialize() => NamedMessages.Register();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), "SetInstanceValuesBackToDefault")]
        private static void OnNetworkTearDown() => NamedMessages.Unregister();

        // Host enables the tweaks for itself as soon as the lobby/round starts.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        private static void OnLobbyStart(StartOfRound __instance)
        {
            BeltBagPatch.Enabled = __instance.IsServer;
        }

        // Host tells each connecting client it has the mod (server-side event).
        // __0 is the connecting client's id (first parameter of OnClientConnect).
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
        private static void OnClientConnected(StartOfRound __instance, ulong __0)
        {
            if (!__instance.IsServer) return;
            NamedMessages.SendHostPresent(new[] { __0 });
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
        private static void OnMainMenu() => BeltBagPatch.Enabled = false;
    }
}

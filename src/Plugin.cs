using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace ImprovedBeltBag
{
    /// <summary>
    /// Main BepInEx plugin. Host-authoritative: bag tweaks stay off on a client until the host
    /// confirms it has the mod (see <see cref="Networking.NamedMessages"/>), so a lone modded
    /// client gets no advantage. The host validates every add server-side.
    /// </summary>
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "Iron.ImprovedBeltBag";
        public const string PLUGIN_NAME = "ImprovedBeltBag";
        public const string PLUGIN_VERSION = "1.0.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            try
            {
                PluginConfig.Init(Config);

                _harmony = new Harmony(PLUGIN_GUID);
                _harmony.PatchAll(typeof(Plugin).Assembly);

                Log.LogInfo($"{PLUGIN_NAME} v{PLUGIN_VERSION} loaded.");
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to initialize; the mod is inactive: {e}");
            }
        }
    }
}

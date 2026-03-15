using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace FM2_PersonalMod
{
    // TODO Review __instance file and update to your own requirements.

    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class FM2_PersonalModPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.Taba.FM2_PersonalMod";
        private const string PluginName = "FM2_PersonalMod";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log = new ManualLogSource(PluginName);

        /// <summary>
        /// Initialise the configuration settings and patch methods
        /// </summary>
        private void Awake()
        {
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");
            Utils.Helpers.Images = Utils.Helpers.LoadReplacementSprites();
            Harmony.PatchAll();
            Logger.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");

            // Sets up our static Log, so it can be used elsewhere in code.
            // .e.g.
            // FM2_PersonalModPlugin.Log.LogDebug("Debug Message to BepInEx log file");
            Log = Logger;
        }
    }
}

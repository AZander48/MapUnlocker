using System.Reflection;
using HarmonyLib;
using GlobalSettings;

namespace MapUnlocker
{
    public class OnStartManager
    {
        public OnStartManager(
            BepInEx.Logging.ManualLogSource logger,
            MapUnlocker plugin
        )
        {
            this.Logger = logger;
            this.plugin = plugin;
        }

        private BepInEx.Logging.ManualLogSource Logger;
        private MapUnlocker plugin;
        

        /// Code below is based on Skydorm's AbilitiesUnlocked Mod (https://thunderstore.io/c/hollow-knight-silksong/p/Skydorm/AbilitiesUnlocked/)
        // Harmony patch to save maps at the start of the game
        [HarmonyPatch]
        internal static class HeroController_Start_Patch
        {
            // gets the method that allows the player to start controlling the player
            private static MethodBase TargetMethod()
            {
                System.Type type = AccessTools.TypeByName("HeroController");
                if (type == null)
                {
                    throw new System.Exception("Could not find type HeroController");
                }
                return AccessTools.Method(type, "Start", (System.Type[])null, (System.Type[])null);
            }
            
            // modifies start method after herocontroller initializes
            private static void Postfix(object __instance)
            {
                // gets mod instance to run mod functions
                var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();
                if (plugin != null)
                {
                    // saves map data before mod could modify it
                    if (plugin.resetMapsAfterLeaving.Value)
                    {
                        plugin.resetManager.StoreOriginalDataOnce();
                        if (plugin.debugMode.Value) plugin.Logger.LogInfo("Stored original map data from HeroController_Start");
                    }

                    // unlocks all maps at the start 
                    if (plugin.unlockAllMapsAtStart != null && plugin.unlockAllMapsAtStart.Value)
                    {
                        plugin.configUI.OnConfigChangedAllMaps(true);
                        if (plugin.debugMode.Value) plugin.Logger.LogInfo("UnlockAtStart finished.");
                    }
                    // enables all maps already unlocked based on playerData map data
                    else
                    {
                        plugin.configUI.ChangeConfigDataOnStart();
                        if (plugin.debugMode.Value) plugin.Logger.LogInfo("Stored config data from HeroController_Start");
                    }

                    if (plugin.hasQuill.Value)
                    {
                        plugin.changeQuillBool(true);
                        if (plugin.debugMode.Value) plugin.Logger.LogInfo("enabled quill from HeroController_Start");
                    }

                }
                else
                {
                    throw new System.Exception("Could not find MapUnlocker mod.");
                }
            }
        }
    }
}
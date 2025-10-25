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
                var plugin = MapUnlocker.Instance;
                if (plugin != null)
                {
                    // saves map data before mod could modify it
                    if (plugin.configUI.resetMapsAfterLeaving?.Value == true)
                    {
                        plugin.resetManager.StoreOriginalData();
                        plugin.Logger.LogInfo("Stored original map data from HeroController_Start");
                    }

                    // unlocks all maps at the start 
                    if (plugin.configUI.unlockAllMapsAtStart?.Value == true)
                    {
                        plugin.configUI.OnConfigChangedAllMaps(true);
                        plugin.Logger.LogInfo("UnlockAtStart finished.");
                    }
                    // enables all maps already unlocked based on playerData map data
                    else
                    {
                        plugin.configUI.ChangeMapsConfigDataOnStart();
                        plugin.Logger.LogInfo("Stored config data from HeroController_Start");
                    }

                    // unlocks all pins at the start 
                    if (plugin.configUI.unlockAllPinsAtStart?.Value == true)
                    {
                        plugin.configUI.OnConfigChangedAllPins(true);
                        plugin.Logger.LogInfo("UnlockAtStart finished.");
                    }
                    // enables all pins already unlocked based on playerData pin data
                    else
                    {
                        plugin.configUI.ChangePinsConfigDataOnStart();
                        plugin.Logger.LogInfo("Stored config data from HeroController_Start");
                    }

                    if (plugin.configUI.hasQuill?.Value == true)
                    {
                        plugin.changeQuillBool(true);
                        plugin.Logger.LogInfo("enabled quill from HeroController_Start");
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
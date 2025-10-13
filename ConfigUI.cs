using BepInEx.Configuration;
using UnityEngine;
using BepInEx;
using System.Reflection;
using System.Linq;
using GlobalSettings;

namespace MapUnlocker
{
    public class ConfigUI
    {
        public ConfigUI(
            BepInEx.Logging.ManualLogSource logger,
            ResetManager resetManager,
            MapUnlocker plugin)
        {
            this.Logger = logger;
            this.resetManager = resetManager;
            this.plugin = plugin;
            
            // Initialize arrays after plugin is set
            this.originalMapData = new bool[MapUnlocker.mapFields.Length];
            this.moddedMapData = new bool[MapUnlocker.mapFields.Length];
        }

        private BepInEx.Logging.ManualLogSource Logger;
        private ResetManager resetManager;
        private MapUnlocker plugin;
        
        // Access to Config through plugin
        private ConfigFile Config => plugin.Config;

        // Config entries
        private ConfigEntry<bool>? debugMode;
        private ConfigEntry<bool>? resetMapsAfterLeaving;
        private ConfigEntry<bool>? hasQuill;
        private ConfigEntry<bool>? unlockAllMapsAtStart;
        private ConfigEntry<bool>[]? mapConfigs;

        // bool list to hold mapData for resets and saves 
        public bool[] originalMapData;
        public bool[] moddedMapData;

        /*
        * InitializeConfig: initializes the configuration menu binds and ui while applying onClick functionality.
        */
        public void InitializeConfig()
        {
            plugin.debugMode = Config.Bind("Map Unlocker", "Debug mode", false, "Enables log messages for debugging purposes.");
            plugin.resetMapsAfterLeaving = Config.Bind("Map Unlocker", "Reset Maps after leaving", false, "Resets all maps to original state after leaving.");
            plugin.hasQuill = Config.Bind("Map Unlocker", "Has Quill", false, "Enables the quill tool.");
            plugin.unlockAllMapsAtStart = Config.Bind("Map Unlocker", "Unlock All Maps At Start", false, "Unlock All Maps At the Start");

            debugMode = plugin.debugMode;
            resetMapsAfterLeaving = plugin.resetMapsAfterLeaving;
            hasQuill = plugin.hasQuill;
            unlockAllMapsAtStart = plugin.unlockAllMapsAtStart;

            // Config entries for each map
            plugin.mapConfigs = new ConfigEntry<bool>[] {
                Config.Bind("Map Unlocker", "Unlock All Maps Now", false, "Unlock All Maps"),
                Config.Bind("Map Unlocker", "Unlock the Map for Moss Grotto", false, "Unlock Moss Grotto"),
                Config.Bind("Map Unlocker", "Unlock the Map for Wilds", false, "Unlock Wilds"),
                Config.Bind("Map Unlocker", "Unlock the Map for Bone forest", false, "Unlock Bone forest"),
                Config.Bind("Map Unlocker", "Unlock the Map for Docks", false, "Unlock Docks"),
                Config.Bind("Map Unlocker", "Unlock the Map for Greymoor", false, "Unlock Greymoor"),
                Config.Bind("Map Unlocker", "Unlock the Map for Bellhart", false, "Unlock Bellhart"),
                Config.Bind("Map Unlocker", "Unlock the Map for Shellwood", false, "Unlock Shellwood"),
                Config.Bind("Map Unlocker", "Unlock the Map for Crawl", false, "Unlock Crawl"),
                Config.Bind("Map Unlocker", "Unlock the Map for Hunters Nest", false, "Unlock Hunters Nest"),
                Config.Bind("Map Unlocker", "Unlock the Map for Judge Steps", false, "Unlock Judge Steps"),
                Config.Bind("Map Unlocker", "Unlock the Map for Dustpens", false, "Unlock Dustpens"),
                Config.Bind("Map Unlocker", "Unlock the Map for Slab", false, "Unlock Slab"),
                Config.Bind("Map Unlocker", "Unlock the Map for Peak", false, "Unlock Peak"),
                Config.Bind("Map Unlocker", "Unlock the Map for Citadel Understore", false, "Unlock Citadel Understore"),
                Config.Bind("Map Unlocker", "Unlock the Map for Coral", false, "Unlock Coral"),
                Config.Bind("Map Unlocker", "Unlock the Map for Swamp", false, "Unlock Swamp"),
                Config.Bind("Map Unlocker", "Unlock the Map for Clover", false, "Unlock Clover"),
                Config.Bind("Map Unlocker", "Unlock the Map for Abyss", false, "Unlock Abyss"),
                Config.Bind("Map Unlocker", "Unlock the Map for Hang", false, "Unlock Hang"),
                Config.Bind("Map Unlocker", "Unlock the Map for SongGate", false, "Unlock SongGate"),
                Config.Bind("Map Unlocker", "Unlock the Map for Halls", false, "Unlock Halls"),
                Config.Bind("Map Unlocker", "Unlock the Map for Ward", false, "Unlock Ward"),
                Config.Bind("Map Unlocker", "Unlock the Map for Cog", false, "Unlock Cog"),
                Config.Bind("Map Unlocker", "Unlock the Map for Library", false, "Unlock Library"),
                Config.Bind("Map Unlocker", "Unlock the Map for Cradle", false, "Unlock Cradle"),
                Config.Bind("Map Unlocker", "Unlock the Map for Arborium", false, "Unlock Arborium"),
                Config.Bind("Map Unlocker", "Unlock the Map for Aqueduct", false, "Unlock Aqueduct"),
                Config.Bind("Map Unlocker", "Unlock the Map for Weavehome", false, "Unlock Weavehome")
            };
            
            mapConfigs = plugin.mapConfigs;

            // Add this line to explicitly create the config file
            Config.Save();

            resetMapsAfterLeaving.SettingChanged += (sender, args) => {
                resetManager.hasStoredOriginalData = false;
                // stores mapData to reset to when leaving
                if (resetMapsAfterLeaving.Value)
                {
                    resetManager.StoreOriginalDataOnce();
                }
            };

            hasQuill.SettingChanged += (sender, args) => plugin.changeQuillBool(hasQuill.Value);

            // apply 'unlock/lock all maps' to onClick/onConfigChanged functionalty to all maps config.
            mapConfigs[MapUnlocker.ALL_MAPS].SettingChanged += (sender, args) => OnConfigChangedAllMaps(mapConfigs[MapUnlocker.ALL_MAPS].Value);

            // applies 'unlock/lock map' to onClick/onConfigChanged functionalty to all maps config.
            for (int map = 1; map < mapConfigs.Length; map++) {
                int currentMap = map;
                mapConfigs[currentMap].SettingChanged += (sender, args) => OnConfigChangedMap(MapUnlocker.mapFields[currentMap], mapConfigs[currentMap].Value);
            }

            if (debugMode!.Value) Logger.LogInfo($"Configuration initialized with {mapConfigs!.Length} map configs");
        }

        /*
        * ChangeConfigDataOnStart: Changes both config data to what maps are enabled already loading into a save.
        */
        public void ChangeConfigDataOnStart()
        {
            if (PlayerData.instance != null)
            {
            for (int i = 0; i < MapUnlocker.mapFields.Length && i < mapConfigs.Length; i++)
            {
                // gets the map field from playerData
                FieldInfo field = PlayerData.instance.GetType().GetField(MapUnlocker.mapFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // enables/disables config based on playerData 
                        mapConfigs![i].Value = currentValue;
                        if (debugMode!.Value) Logger.LogInfo($"mapData {MapUnlocker.mapFields[i]}: {mapConfigs![i].Value}");

                    }
                }
            } else {
                if (debugMode!.Value) Logger.LogInfo("(PlayerData.instance is null");
            }
        }

        /*
    * OnConfigChangedAllMaps: Unlocks or locks all maps within the mapFields list
    * value: the value to change map field values to. 
    */
    public void OnConfigChangedAllMaps(bool value)
    {
        string action = value ? "Unlocked" : "Locked";

        if (PlayerData.instance != null)
        {
            for (int map = 0; map < MapUnlocker.mapFields.Length; map++)
            {
                if (plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.mapFields[map], value))
                {
                    mapConfigs![map].Value = value;
                    if (debugMode!.Value) Logger.LogInfo(MapUnlocker.mapFields[map] + " has been " + action + "!");
                }
                else
                {
                    if (debugMode!.Value) Logger.LogInfo(MapUnlocker.mapFields[map] + " could not be " + action + "! Skipping map...");
                }
            }
        }
        else
        {
            if (debugMode!.Value) Logger.LogInfo("PlayerData could not be found");
        }
    }

    /*
    * OnConfigChangedMap: Locks or unlocks a map based on the mapName field.
    * mapName: The playerData field name to target.
    * value: Determines if the map should be unlocked or not.
    */
    private void OnConfigChangedMap(string mapName, bool value)
    {
        // lock or unlocking string message purely for debugging purposes
        string action = value ? "Unlocked" : "Locked";

        if (PlayerData.instance != null)
        {
            // checks an logs if modifying the map data field was successful or not
            if (plugin.SetPlayerDataBool(PlayerData.instance, mapName, value))
            {
                if (!value) {
                    plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.mapFields[MapUnlocker.ALL_MAPS], value);
                    mapConfigs![MapUnlocker.ALL_MAPS].Value = value;
                }
                
            } else {
                if (debugMode!.Value) Logger.LogInfo(mapName+" could not be "+action+"!");
            }
        }
    }
    }
}
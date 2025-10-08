using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using Microsoft.CodeAnalysis;

namespace MapUnlocker;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class MapUnlocker : BaseUnityPlugin
{
    // Use the inherited Logger property instead of hiding it
    private Harmony harmony;

    // map list index constants
    static readonly int ALL_MAPS = 0;

    //
    private ConfigEntry<bool> debugMode;

    //
    private ConfigEntry<bool> resetMapsAfterLeaving;

    //
    private ConfigEntry<bool> unlockAllMapsAtStart;
    public ConfigEntry<bool>[] mapConfigs = new ConfigEntry<bool>[mapFields.Length];

    private bool hasStoredOriginalData = false;

    public bool[] originalMapData = new bool[mapFields.Length];
    public bool[] moddedMapData = new bool[mapFields.Length];

    // List of all map fields to unlock from within playerData
    public static readonly string[] mapFields = {
        "mapAllRooms", 
        "HasMossGrottoMap",
        "HasWildsMap",
        "HasBoneforestMap",
        "HasDocksMap",
        "HasGreymoorMap",
        "HasBellhartMap",
        "HasShellwoodMap",
        "HasCrawlMap",
        "HasHuntersNestMap",
        "HasJudgeStepsMap",
        "HasDustpensMap",
        "HasSlabMap",
        "HasPeakMap",
        "HasCitadelUnderstoreMap",
        "HasCoralMap",
        "HasSwampMap",
        "HasCloverMap",
        "HasAbyssMap",
        "HasHangMap",
        "HasSongGateMap",
        "HasHallsMap",
        "HasWardMap",
        "HasCogMap",
        "HasLibraryMap",
        "HasCradleMap",
        "HasArboriumMap",
        "HasAqueductMap",
        "HasWeavehomeMap"
    };

    

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        Logger.LogInfo($"Plugin version: {PluginInfo.PLUGIN_VERSION}");

        // Initialize Harmony
        harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();

        // Initialize configuration
        InitializeConfig();
        
        Logger.LogInfo("Map Unlocker mod initialization completed");
    }

    /*
    * InitializeConfig: initializes the configuration menu binds and ui while applying onClick functionality.
    */
    private void InitializeConfig()
    {
        debugMode = Config.Bind("Map Unlocker", "Debug mode", false, "Enables log messages for debugging purposes.");
        resetMapsAfterLeaving = Config.Bind("Map Unlocker", "Reset Maps after leaving", false, "Resets all maps to original state after leaving.");
        unlockAllMapsAtStart = Config.Bind("Map Unlocker", "Unlock All Maps At Start", false, "Unlock All Maps At the Start");

        // Config entries for each map
        mapConfigs = new ConfigEntry<bool>[] {
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

        // Add this line to explicitly create the config file
        Config.Save();

        resetMapsAfterLeaving.SettingChanged += (sender, args) => {
            hasStoredOriginalData = false;
            if (resetMapsAfterLeaving.Value) {
                StoreOriginalDataOnce();
            }
        };

        // apply 'unlock/lock all maps' to onClick/onConfigChanged functionalty to all maps config.
        mapConfigs[ALL_MAPS].SettingChanged += (sender, args) => OnConfigChangedAllMaps(mapConfigs[ALL_MAPS].Value);

        // applies 'unlock/lock map' to onClick/onConfigChanged functionalty to all maps config.
        for (int map = 1; map < mapConfigs.Length; map++) {
            int currentMap = map;
            mapConfigs[currentMap].SettingChanged += (sender, args) => OnConfigChangedMap(mapFields[currentMap], mapConfigs[currentMap].Value);
        }

        if (debugMode.Value) Logger.LogInfo($"Configuration initialized with {mapConfigs.Length} map configs");
    }


    private void StoreMapData(bool[] mapData)
    {
        if (PlayerData.instance != null && mapData != null)
        {
            for (int i = 0; i < mapFields.Length && i < mapData.Length; i++)
            {
                FieldInfo field = PlayerData.instance.GetType().GetField(mapFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    bool currentValue = (bool)field.GetValue(PlayerData.instance);

                    // Actually store the value in the array
                    mapData[i] = currentValue;
                    if (debugMode.Value) Logger.LogInfo($"mapData {mapFields[i]}: {mapData[i]}");

                }
            }
        }
    }


    private void RestoreMapData(bool[] mapData) 
    {
        if (PlayerData.instance != null && mapData != null)
        {
            if (debugMode.Value) Logger.LogInfo("RestoreMapData Start --------------------------------");
            for (int i = 0; i < mapFields.Length && i < mapData.Length; i++)
            {
                if (SetPlayerDataBool(PlayerData.instance, mapFields[i], mapData[i]) && debugMode.Value)
                {
                    Logger.LogInfo($"{mapFields[i]} -> {mapData[i]}!");
                }
            }
            if (debugMode.Value) Logger.LogInfo("Restored map states");
        }
    }

    private void StoreOriginalDataOnce()
    {
        if (PlayerData.instance != null)
        {
            if (!hasStoredOriginalData)
            {
                StoreMapData(originalMapData);
                hasStoredOriginalData = true;
                if (debugMode.Value) Logger.LogInfo("Stored original map data (one-time only)");
                if (debugMode.Value) DebugArrayContents("originalMapData", originalMapData);
            }
        } else {
            if (debugMode.Value) Logger.LogInfo("(PlayerData.instance is null");
        }
    }


    private void ChangeConfigDataOnStart()
    {
        if (PlayerData.instance != null)
        {
            for (int i = 0; i < mapFields.Length && i < mapConfigs.Length; i++)
            {
                FieldInfo field = PlayerData.instance.GetType().GetField(mapFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    bool currentValue = (bool)field.GetValue(PlayerData.instance);

                    // Actually store the value in the array
                    mapConfigs[i].Value = currentValue;
                    if (debugMode.Value) Logger.LogInfo($"mapData {mapFields[i]}: {mapConfigs[i].Value}");

                }
            }
        } else {
            if (debugMode.Value) Logger.LogInfo("(PlayerData.instance is null");
        }
    }


    private void OnDestroy()
    {
        // Clean up Harmony patches when mod is unloaded
        harmony?.UnpatchSelf();
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
            for (int map = 0; map < mapFields.Length; map++)
            {
                if (SetPlayerDataBool(PlayerData.instance, mapFields[map], value))
                {
                    mapConfigs[map].Value = value;
                    if (debugMode.Value) Logger.LogInfo(mapFields[map]+" has been "+action+"!");
                } else {
                    if (debugMode.Value) Logger.LogInfo(mapFields[map]+" could not be "+action+"! Skipping map...");
                }
            }
        } else {
            if (debugMode.Value) Logger.LogInfo("PlayerData could not be found");
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
            if (SetPlayerDataBool(PlayerData.instance, mapName, value))
            {
                if (!value) {
                    SetPlayerDataBool(PlayerData.instance, mapFields[ALL_MAPS], value);
                    mapConfigs[ALL_MAPS].Value = value;
                }
                
            } else {
                if (debugMode.Value) Logger.LogInfo(mapName+" could not be "+action+"!");
            }
        }
    }

    /*
    * SetPlayerDataBool: changes the value of a playerData field based on the value.
    * playerData: The player's data instance.
    * fieldName: The name of the playerData field that needs to be changed.
    * value: the value to change the playerData field's value to.
    */
    private bool SetPlayerDataBool(object playerData, string fieldName, bool value)
    {
        try
        {
            // get the field within the playerData instance based on fieldName
            FieldInfo field = playerData.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
            // checks if field exists or is contains a boolean for value
            if (field != null && field.FieldType == typeof(bool))
            {
                field.SetValue(playerData, value);
                return true;
            }
            else
            {
                if (debugMode.Value) Logger.LogWarning($"Field {fieldName} not found or not boolean");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            if (debugMode.Value) Logger.LogError($"Error setting {fieldName}: {ex.Message}");
            return false;
        }
    }


    private void DebugArrayContents(string arrayName, bool[] array)
    {
        Logger.LogInfo($"=== {arrayName} Contents ===");
        for (int i = 0; i < array.Length; i++)
        {
            Logger.LogInfo($"{arrayName}[{i}] = {array[i]} ({mapFields[i]})");
        }
        Logger.LogInfo($"=== End {arrayName} ===");
    }

    /// Code below is based on Skydorm's AbilitiesUnlocked Mod (https://thunderstore.io/c/hollow-knight-silksong/p/Skydorm/AbilitiesUnlocked/)
    // Harmony patch to save maps at the start of the game
    [HarmonyPatch]
    internal static class HeroController_Start_Patch
    {
        private static MethodBase TargetMethod()
        {
            System.Type type = AccessTools.TypeByName("HeroController");
            if (type == null)
            {
                throw new System.Exception("Could not find type HeroController");
            }
            return AccessTools.Method(type, "Start", (System.Type[])null, (System.Type[])null);
        }
        
        private static void Postfix(object __instance)
        {
            var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();
            if (plugin != null) {
                if (plugin.resetMapsAfterLeaving.Value)
                {
                    plugin.StoreOriginalDataOnce();
                    if (plugin.debugMode.Value) plugin.Logger.LogInfo("Stored original map data from HeroController_Start");
                }

                if (plugin.unlockAllMapsAtStart != null && plugin.unlockAllMapsAtStart.Value)
                {
                    plugin.OnConfigChangedAllMaps(true);
                    if (plugin.debugMode.Value) plugin.Logger.LogInfo("UnlockAtStart finished.");
                }
                else
                {
                    plugin.ChangeConfigDataOnStart();
                    if (plugin.debugMode.Value) plugin.Logger.LogInfo("Stored config data from HeroController_Start");
                }
                
            } else {
                throw new System.Exception("Could not find MapUnlocker mod.");
            }
        }
    }
    
    
    [HarmonyPatch]
    internal static class SaveGameData_Constructor_Patch
    {
        private static MethodBase TargetMethod()
        {
            System.Type type = AccessTools.TypeByName("SaveGameData");
            if (type != null)
            {
                return AccessTools.Constructor(type, new System.Type[] { 
                    AccessTools.TypeByName("PlayerData"), 
                    AccessTools.TypeByName("SceneData") 
                });            
            }
            return null;
        }

        private static void Prefix(PlayerData playerData, SceneData sceneData)
        {
            var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();
            if (plugin != null && plugin.resetMapsAfterLeaving.Value)
            {
                plugin.StoreMapData(plugin.moddedMapData);
                if (plugin.debugMode.Value) plugin.DebugArrayContents("originalMapData", plugin.originalMapData);
                plugin.RestoreMapData(plugin.originalMapData);
                if (plugin.debugMode.Value) plugin.Logger.LogInfo("Restored original map states before SaveGameData constructor");
            }
        }

        private static void Postfix(PlayerData playerData, SceneData sceneData)
        {
            var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();
            if (plugin != null && plugin.resetMapsAfterLeaving.Value)
            {
                plugin.RestoreMapData(plugin.moddedMapData);
                if (plugin.debugMode.Value) plugin.Logger.LogInfo("Re-applied mod states after SaveGameData constructor");
            }
        }
    }
}
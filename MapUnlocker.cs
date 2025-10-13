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
    private Harmony harmony = null!;
    
    // Make Logger accessible to managers
    public new BepInEx.Logging.ManualLogSource Logger => base.Logger;

    // map list index constants
    public static readonly int ALL_MAPS = 0;

    // Manager instances
    public ResetManager resetManager = null!;
    public ConfigUI configUI = null!;
    private OnStartManager onStartManager = null!;

    public ConfigEntry<bool> debugMode = null!;

    // config entry to enable and disable reseting maps to the original map data when leaving
    public ConfigEntry<bool> resetMapsAfterLeaving = null!;

    public ConfigEntry<bool> hasQuill = null!;

    // Config entry to enable all maps at the start of a game
    public ConfigEntry<bool> unlockAllMapsAtStart = null!;
    // Config entry list to modify each entry with ease
    public ConfigEntry<bool>[] mapConfigs = null!;

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

        // Initialize managers
        resetManager = new ResetManager(Logger, this);
        configUI = new ConfigUI(Logger, resetManager, this);
        onStartManager = new OnStartManager(Logger, this);

        // Initialize configuration
        configUI.InitializeConfig();
        
        Logger.LogInfo("Map Unlocker mod initialization completed");
    }


    private void OnDestroy()
    {
        // Clean up Harmony patches when mod is unloaded
        harmony?.UnpatchSelf();
    }

    /*
    * SetPlayerDataBool: changes the value of a playerData field based on the value.
    * playerData: The player's data instance.
    * fieldName: The name of the playerData field that needs to be changed.
    * value: the value to change the playerData field's value to.
    */
    public bool SetPlayerDataBool(object playerData, string fieldName, bool value)
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


    public void changeQuillBool(bool value)
    {
        string action = hasQuill.Value ? "Enabled" : "Disabled";
        if (SetPlayerDataBool(PlayerData.instance, "hasQuill", hasQuill.Value))
        {
            Logger.LogInfo($"{action} Quill.");
        }
    }

    /*
    * StoreMapData: Stores map data from playerData into a bool list
    * mapData: the bool list that stores the playerData map data
    */
    public void StoreMapData(bool[] mapData)
    {
        if (PlayerData.instance != null && mapData != null)
        {
            for (int i = 0; i < mapFields.Length && i < mapData.Length; i++)
            {
                // gets the map field from playerData
                FieldInfo field = PlayerData.instance.GetType().GetField(mapFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field != null && field.FieldType == typeof(bool))
                {
                    bool currentValue = (bool)field.GetValue(PlayerData.instance);

                    // Stores the value in the array
                    mapData[i] = currentValue;
                    if (debugMode != null && debugMode.Value) Logger.LogInfo($"mapData {mapFields[i]}: {mapData[i]}");
                }
            }
        }
    }


    /*
    * DebugArrayContents: Logs the bool list and its contents into terminal
    * arrayName: name of bool list variable
    * array: bool list to log
    */
    public void DebugArrayContents(string arrayName, bool[] array)
    {
        Logger.LogInfo($"=== {arrayName} Contents ===");
        for (int i = 0; i < array.Length; i++)
        {
            Logger.LogInfo($"{arrayName}[{i}] = {array[i]} ({mapFields[i]})");
        }
        Logger.LogInfo($"=== End {arrayName} ===");
    }
}
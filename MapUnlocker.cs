using System;
using System.Diagnostics;
using System.Linq;
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
    public static MapUnlocker Instance { get; private set; } = null!;

    // map list index constants
    public static readonly int ALL_FIELDS = 0;
    public static readonly int MAPS = 0;
    public static readonly int PINS = 1;
    public static readonly int MARKERS = 2;

    // Manager instances
    public ResetManager resetManager = null!;
    public ConfigUI configUI = null!;
    private OnStartManager onStartManager = null!;

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

    public static readonly string[] pinFields =
    {
        "hasPinBench",
        "hasPinCocoon",
        "hasPinShop",
        "hasPinSpa",
        "hasPinStag",
        "hasPinTube",
        "hasPinFleaMarrowlands",
        "hasPinFleaMidlands",
        "hasPinFleaBlastedlands",
        "hasPinFleaCitadel",
        "hasPinFleaPeaklands",
        "hasPinFleaMucklands"
    };

    public static readonly string[] markerFields =
    {
        "hasMarker",
        "hasMarker_a",
        "hasMarker_b",
        "hasMarker_c",
        "hasMarker_d",
        "hasMarker_e"
    };

    // FieldInfo objects to reference PlayerData fields dynamically
    public static readonly FieldInfo[][] playerDataFieldsBools =
    [
        mapFields.Select(fieldName => 
            typeof(PlayerData).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ).ToArray(),
        pinFields.Select(fieldName => 
            typeof(PlayerData).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ).ToArray(),
        markerFields.Select(fieldName => 
            typeof(PlayerData).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        ).ToArray()
    ];
    

    private void Awake()
    {
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} has loaded!");
        Logger.LogInfo($"Plugin version: {PluginInfo.PLUGIN_VERSION}");

        // Set the static instance for patches to access
        Instance = this;

        // Initialize managers first
        resetManager = new ResetManager(Logger, this);
        configUI = new ConfigUI(Logger, resetManager, this);
        
        // Set the configUI reference in resetManager after it's created
        resetManager.SetConfigUI(configUI);

        // Initialize configuration BEFORE applying Harmony patches
        configUI.InitializeConfig();

        // Initialize Harmony after config is ready
        harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        Logger.LogInfo("Harmony patches applied successfully!");

        // Initialize remaining managers
        onStartManager = new OnStartManager(Logger, this);
        
        Logger.LogInfo("Map Unlocker mod initialization completed");
    }


    private void OnDestroy()
    {
        // Clean up Harmony patches when mod is unloaded
        harmony?.UnpatchSelf();
    }


    // Helper method to get the bool value from a FieldInfo
    public static bool GetPlayerDataBoolValue(int category, int index)
    {
        if (PlayerData.instance == null ||
            category < 0 || category >= playerDataFieldsBools.Length ||
            index < 0 || index >= playerDataFieldsBools[category].Length ||
            playerDataFieldsBools[category][index] == null)
        {
            return false;
        }

        var field = playerDataFieldsBools[category][index];
        if (field.FieldType == typeof(bool))
        {
            return (bool)field.GetValue(PlayerData.instance);
        }
        return false;
    }

    public static bool? GetPlayerDataBoolValue(FieldInfo fieldInfo)
    {
        if (PlayerData.instance != null && fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(bool))
            {
                return (bool)fieldInfo.GetValue(PlayerData.instance);
            }
        }

        return null;
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
                Logger.LogInfo($"{fieldName}: {value}");
                return true;
            }
            else
            {
                if (configUI.debugMode?.Value == true) Logger.LogWarning($"Field {fieldName} not found or not boolean");
                return false;
            }
        }
        catch (Exception ex)
        {
            if (configUI.debugMode?.Value == true) Logger.LogError($"Error setting {fieldName}: {ex.Message}");
            return false;
        }
    }
    

    public bool SetPlayerDataBool(FieldInfo fieldInfo, bool value)
    {
        if (PlayerData.instance != null && fieldInfo != null)
        {
            if (fieldInfo.FieldType == typeof(bool))
            {
                fieldInfo.SetValue(PlayerData.instance, value);
                if (configUI.debugMode?.Value == true) Logger.LogInfo($"{fieldInfo.Name}: {value}");
                return true;
            }

            
            if (configUI.debugMode?.Value == true) Logger.LogInfo($"{fieldInfo.Name} is not a boolean.");
        }

        return false;
    }


    public void changeQuillBool(bool value)
    {
        string action = configUI.hasQuill.Value ? "Enabled" : "Disabled";
        if (SetPlayerDataBool(PlayerData.instance, "hasQuill", configUI.hasQuill.Value))
        {
            Logger.LogInfo($"{action} Quill.");
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
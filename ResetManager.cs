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
using Unity.Mathematics;

namespace MapUnlocker
{
    public class ResetManager
    {
        private BepInEx.Logging.ManualLogSource Logger;
        private MapUnlocker plugin;
        private ConfigUI configUI;

        // bool list to hold mapData for resets and saves 
        public static bool[] originalMapData = new bool[MapUnlocker.mapFields.Length];
        public static bool[] moddedMapData = new bool[MapUnlocker.mapFields.Length];
        public static bool[] originalPinData = new bool[MapUnlocker.pinFields.Length];
        public static bool[] moddedPinData = new bool[MapUnlocker.pinFields.Length];
        public static bool[] originalMarkerData = new bool[MapUnlocker.pinFields.Length];
        public static bool[] moddedMarkerData = new bool[MapUnlocker.pinFields.Length];

        public ResetManager(BepInEx.Logging.ManualLogSource logger, MapUnlocker plugin)
        {
            this.Logger = logger;
            this.plugin = plugin;
        }

        public void SetConfigUI(ConfigUI configUI)
        {
            this.configUI = configUI;
        }

        /*
        * StoreBoolData: Stores pin data from playerData into a bool list
        * boolData: the bool list that stores the playerData map data
        * playerDataFields: 
        */
        public void StoreBoolData(bool[] boolData, string[] playerDataFields)
        {
            if (PlayerData.instance != null && boolData != null)
            {
                for (int i = 0; i < playerDataFields.Length && i < boolData.Length; i++)
                {
                    // gets the map field from playerData
                    FieldInfo field = PlayerData.instance.GetType().GetField(playerDataFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // Stores the value in the array
                        boolData[i] = currentValue;
                        if (configUI.debugMode?.Value == true) Logger.LogInfo($"{playerDataFields[i]}: {boolData[i]}");
                    }
                }


                if (configUI.debugMode?.Value == true)
                {
                    Logger.LogInfo("Stored original data");
                    plugin.DebugArrayContents("originalBoolData", boolData);
                }
            }
            else
            {
                Logger.LogInfo("(PlayerData.instance is null or configUI not initialized");
            }
        }
        

        /*
        * StoreBoolData: Stores pin data from playerData into a bool list
        * boolData: the bool list that stores the playerData map data
        * playerDataFields: 
        */
        public void StoreBoolData(bool[] boolData, FieldInfo[] playerDataFields)
        {
            if (PlayerData.instance != null && boolData != null)
            {
                for (int i = 0; i < playerDataFields.Length && i < boolData.Length; i++)
                {
                    // gets the map field from playerData
                    var currentValue = MapUnlocker.GetPlayerDataBoolValue(playerDataFields[i]);

                    if (currentValue != null)
                    {
                        // Stores the value in the array
                        boolData[i] = (bool)currentValue;
                        if (configUI.debugMode?.Value == true) Logger.LogInfo($"{playerDataFields[i]}: {boolData[i]}");
                    } else
                    {
                        Logger.LogError($"Could not get playerDataField value {i}!");
                    }
                }

                
                if (configUI.debugMode?.Value == true)
                {
                    Logger.LogInfo("Stored original data");
                    plugin.DebugArrayContents("originalBoolData", boolData);
                } 
            } else
            {
                Logger.LogInfo("(PlayerData.instance is null or configUI not initialized");
            }
        }


        /*
        * OverwriteBoolData: Applys map data from bool list to playerData's map data
        * boolData: the bool list that is used to overwrite.
        * playerDataFields: 
        */
        public void OverwriteBoolData(bool[] boolData, string[] playerDataFields)
        {
            if (PlayerData.instance != null && boolData != null && configUI != null)
            {
                if (configUI.debugMode?.Value == true) Logger.LogInfo("OverwriteMapData Start --------------------------------");
                for (int i = 0; i < playerDataFields.Length && i < boolData.Length; i++)
                {
                    // sets the playerData field based on pinData field
                    if (plugin.SetPlayerDataBool(PlayerData.instance, playerDataFields[i], boolData[i]) && configUI.debugMode?.Value == true)
                    {
                        Logger.LogInfo($"{playerDataFields[i]} -> {boolData[i]}!");
                    }
                }
                if (configUI.debugMode?.Value == true) Logger.LogInfo("Restored pin states");
            }
        }

        /*
        * OverwriteBoolData: Applys map data from bool list to playerData's map data
        * boolData: the bool list that is used to overwrite.
        * playerDataFields: 
        */
        public void OverwriteBoolData(bool[] boolData, FieldInfo[] playerDataFields)
        {
            if (PlayerData.instance != null && boolData != null && configUI != null)
            {
                if (configUI.debugMode?.Value == true) Logger.LogInfo("OverwriteBoolData Start --------------------------------");
                for (int i = 0; i < playerDataFields.Length && i < boolData.Length; i++)
                {
                    // sets the playerData field based on pinData field
                    if (
                        plugin.SetPlayerDataBool(playerDataFields[i], boolData[i]) 
                        && configUI.debugMode?.Value == true
                        )
                    {
                        Logger.LogInfo($"playerDataFields: {playerDataFields[i]} -> {boolData[i]}!");
                    }
                }
                if (configUI.debugMode?.Value == true) Logger.LogInfo("Restored pin states");
            }
        }

        /*
        * StoreOriginalData: Stores the platerData map data into the original map data bool list.
        */
        public void StoreOriginalData()
        {
            if (PlayerData.instance != null && configUI != null)
            {
                StoreBoolData(originalMapData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS]);
                StoreBoolData(originalPinData, MapUnlocker.playerDataFieldsBools[MapUnlocker.PINS]);
                StoreBoolData(originalMarkerData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS]);
            }
            else
            {
                Logger.LogInfo("(PlayerData.instance is null or configUI not initialized");
            }
        }
        
        /* 
        * modifies saves so that the original map data is saved rather than the modded map data
        */
        [HarmonyPatch]
        internal static class SaveGameData_Constructor_Patch
        {
            // gets the method that saves playerData
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

            // modifies save before it runs
            private static void Prefix(PlayerData playerData, SceneData sceneData)
            {
                // gets mod instance to run mod functions
                var plugin = MapUnlocker.Instance;

                // stores current map data then overwrites playerData map data
                if (plugin != null && plugin.configUI.resetDataAfterLeaving?.Value == true)
                {
                    plugin.resetManager.StoreBoolData(moddedMapData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS]);
                    plugin.resetManager.StoreBoolData(moddedPinData, MapUnlocker.playerDataFieldsBools[MapUnlocker.PINS]);
                    plugin.resetManager.StoreBoolData(moddedMarkerData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS]);

                    if (plugin.configUI?.debugMode.Value == true) plugin.DebugArrayContents("originalMapData", originalMapData);
                    if (plugin.configUI?.debugMode.Value == true) plugin.DebugArrayContents("originalPinData", originalPinData);
                    if (plugin.configUI?.debugMode.Value == true) plugin.DebugArrayContents("originalMarkerData", originalMarkerData);

                    plugin.resetManager.OverwriteBoolData(originalMapData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS]);
                    plugin.resetManager.OverwriteBoolData(originalPinData, MapUnlocker.playerDataFieldsBools[MapUnlocker.PINS]);
                    plugin.resetManager.OverwriteBoolData(originalMarkerData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS]);

                    plugin.Logger.LogInfo("Restored original map states before SaveGameData constructor");
                }
            }

            // modifies save after it runs
            private static void Postfix(PlayerData playerData, SceneData sceneData)
            {
                // gets mod instance to run mod functions
                var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();

                // overwrites current playerData map data with the map data before the save 
                if (plugin != null && plugin.configUI.resetDataAfterLeaving?.Value == true)
                {
                    plugin.resetManager.OverwriteBoolData(moddedMapData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS]);
                    plugin.resetManager.OverwriteBoolData(moddedPinData, MapUnlocker.playerDataFieldsBools[MapUnlocker.PINS]);
                    plugin.resetManager.OverwriteBoolData(moddedMarkerData, MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS]);

                    plugin.Logger.LogInfo("Re-applied mod states after SaveGameData constructor");
                }
            }
        }
    }
}
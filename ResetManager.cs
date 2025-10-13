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

namespace MapUnlocker
{
    public class ResetManager
    {
        private BepInEx.Logging.ManualLogSource Logger;
        private MapUnlocker plugin;

        public ResetManager(BepInEx.Logging.ManualLogSource logger, MapUnlocker plugin)
        {
            this.Logger = logger;
            this.plugin = plugin;
            this.hasStoredOriginalData = false;
        }

        public bool hasStoredOriginalData;
        /*
        * StoreOriginalDataOnce: Stores the platerData map data into the original map data bool list if
        * it wans't stored already.
        */
        public void StoreOriginalDataOnce()
        {
            if (PlayerData.instance != null)
            {
                if (!hasStoredOriginalData)
                {
                    plugin.StoreMapData(plugin.configUI.originalMapData);
                    hasStoredOriginalData = true;

                    if (plugin.debugMode != null && plugin.debugMode.Value) Logger.LogInfo("Stored original map data (one-time only)");
                    if (plugin.debugMode != null && plugin.debugMode.Value) plugin.DebugArrayContents("originalMapData", plugin.configUI.originalMapData);
                }
            }
            else
            {
                if (plugin.debugMode != null && plugin.debugMode.Value) Logger.LogInfo("(PlayerData.instance is null");
            }
        }

        /*
        * OverwriteMapData: Applys map data from bool list to playerData's map data
        * mapData: the bool list that is used to overwrite.
        */
        public void OverwriteMapData(bool[] mapData) 
        {
            if (PlayerData.instance != null && mapData != null)
            {
                if (plugin.debugMode != null && plugin.debugMode.Value) Logger.LogInfo("OverwriteMapData Start --------------------------------");
                for (int i = 0; i < MapUnlocker.mapFields.Length && i < mapData.Length; i++)
                {
                    // sets the playerData field based on mapData field
                    if (plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.mapFields[i], mapData[i]) && plugin.debugMode != null && plugin.debugMode.Value)
                    {
                        Logger.LogInfo($"{MapUnlocker.mapFields[i]} -> {mapData[i]}!");
                    }
                }
                if (plugin.debugMode != null && plugin.debugMode.Value) Logger.LogInfo("Restored map states");
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
                var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();

                // stores current map data then overwrites playerData map data
                if (plugin != null && plugin.resetMapsAfterLeaving.Value)
                {
                    plugin.StoreMapData(plugin.configUI.moddedMapData);
                    if (plugin.debugMode.Value) plugin.DebugArrayContents("originalMapData", plugin.configUI.originalMapData);
                    plugin.resetManager.OverwriteMapData(plugin.configUI.originalMapData);
                    if (plugin.debugMode.Value) plugin.Logger.LogInfo("Restored original map states before SaveGameData constructor");
                }
            }

            // modifies save after it runs
            private static void Postfix(PlayerData playerData, SceneData sceneData)
            {
                // gets mod instance to run mod functions
                var plugin = UnityEngine.Object.FindAnyObjectByType<MapUnlocker>();

                // overwrites current playerData map data with the map data before the save 
                if (plugin != null && plugin.resetMapsAfterLeaving.Value)
                {
                    plugin.resetManager.OverwriteMapData(plugin.configUI.moddedMapData);
                    if (plugin.debugMode.Value) plugin.Logger.LogInfo("Re-applied mod states after SaveGameData constructor");
                }
            }
        }
    }
}
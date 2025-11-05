using BepInEx.Configuration;
using UnityEngine;
using BepInEx;
using System.Reflection;
using System.Linq;
using GlobalSettings;
using UnityEngine.UIElements;
using System.Diagnostics;

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
            
        }

        private BepInEx.Logging.ManualLogSource Logger;
        private ResetManager resetManager;
        private MapUnlocker plugin;
        
        // Access to Config through plugin
        private ConfigFile Config => plugin.Config;

        // Config entries
        public ConfigEntry<bool>? debugMode;
        public ConfigEntry<bool>? resetDataAfterLeaving;
        public ConfigEntry<bool>? hasQuill;
        public ConfigEntry<bool>? unlockAllMapsAtStart;
        public ConfigEntry<bool>? unlockAllMaps;
        public ConfigEntry<bool>[]? mapConfigs;
        public ConfigEntry<bool>? unlockAllPinsAtStart;
        public ConfigEntry<bool>? unlockAllPins;
        public ConfigEntry<bool>[]? pinConfigs;
        public ConfigEntry<bool>? unlockAllMarkersAtStart;
        public ConfigEntry<bool>? unlockAllMarkers;
        public ConfigEntry<bool>[]? markerConfigs;



        private static bool allowChangeAllMaps = true;
        private static bool isChangingAllMaps = false;
        private static bool allowChangeAllPins = true;
        private static bool isChangingAllPins = false;
        private static bool allowChangeAllMarkers = true;
        private static bool isChangingAllMarkers = false;

        /*
        * InitializeConfig: initializes the configuration menu binds and ui while applying onClick functionality.
        */
        public void InitializeConfig()
        {
            debugMode = Config.Bind("Debug", "Debug mode", false, "Enables log messages for debugging purposes.");
            resetDataAfterLeaving = Config.Bind("General", "Reset Maps after leaving", false, "Resets all maps to original state after leaving.");
            
            hasQuill = Config.Bind("General", "Has Quill", false, "Enables the quill tool.");

            unlockAllMapsAtStart = Config.Bind("Maps", "Unlock All Maps At Start", false, "Unlock All Maps At the Start");

            

            // Config entries for each map
            mapConfigs = new ConfigEntry<bool>[] {
                unlockAllMaps = Config.Bind("Maps", "Unlock All Maps Now", false, "Unlock All Maps"),
                Config.Bind("Maps", "Unlock the Map for Moss Grotto", false, "Unlock Moss Grotto"),
                Config.Bind("Maps", "Unlock the Map for Wilds", false, "Unlock Wilds"),
                Config.Bind("Maps", "Unlock the Map for Bone forest", false, "Unlock Bone forest"),
                Config.Bind("Maps", "Unlock the Map for Docks", false, "Unlock Docks"),
                Config.Bind("Maps", "Unlock the Map for Greymoor", false, "Unlock Greymoor"),
                Config.Bind("Maps", "Unlock the Map for Bellhart", false, "Unlock Bellhart"),
                Config.Bind("Maps", "Unlock the Map for Shellwood", false, "Unlock Shellwood"),
                Config.Bind("Maps", "Unlock the Map for Crawl", false, "Unlock Crawl"),
                Config.Bind("Maps", "Unlock the Map for Hunters Nest", false, "Unlock Hunters Nest"),
                Config.Bind("Maps", "Unlock the Map for Judge Steps", false, "Unlock Judge Steps"),
                Config.Bind("Maps", "Unlock the Map for Dustpens", false, "Unlock Dustpens"),
                Config.Bind("Maps", "Unlock the Map for Slab", false, "Unlock Slab"),
                Config.Bind("Maps", "Unlock the Map for Peak", false, "Unlock Peak"),
                Config.Bind("Maps", "Unlock the Map for Citadel Understore", false, "Unlock Citadel Understore"),
                Config.Bind("Maps", "Unlock the Map for Coral", false, "Unlock Coral"),
                Config.Bind("Maps", "Unlock the Map for Swamp", false, "Unlock Swamp"),
                Config.Bind("Maps", "Unlock the Map for Clover", false, "Unlock Clover"),
                Config.Bind("Maps", "Unlock the Map for Abyss", false, "Unlock Abyss"),
                Config.Bind("Maps", "Unlock the Map for Hang", false, "Unlock Hang"),
                Config.Bind("Maps", "Unlock the Map for SongGate", false, "Unlock SongGate"),
                Config.Bind("Maps", "Unlock the Map for Halls", false, "Unlock Halls"),
                Config.Bind("Maps", "Unlock the Map for Ward", false, "Unlock Ward"),
                Config.Bind("Maps", "Unlock the Map for Cog", false, "Unlock Cog"),
                Config.Bind("Maps", "Unlock the Map for Library", false, "Unlock Library"),
                Config.Bind("Maps", "Unlock the Map for Cradle", false, "Unlock Cradle"),
                Config.Bind("Maps", "Unlock the Map for Arborium", false, "Unlock Arborium"),
                Config.Bind("Maps", "Unlock the Map for Aqueduct", false, "Unlock Aqueduct"),
                Config.Bind("Maps", "Unlock the Map for Weavehome", false, "Unlock Weavehome")
            };

            unlockAllPinsAtStart = Config.Bind("Pins", "Unlock All Pins At Start", false, "Unlock All Pins At the Start");

            unlockAllPins = Config.Bind("Pins", "Unlock All Pins Now", false, "Unlock All Pins");

            // Config entries for each pin
            pinConfigs = new ConfigEntry<bool>[] {
                Config.Bind("Pins", "Unlock the Bench Pin", false, "Unlock Bench Pin"),
                Config.Bind("Pins", "Unlock the Cocoon Pin", false, "Unlock Cocoon Pin"),
                Config.Bind("Pins", "Unlock the Shop Pin forest", false, "Unlock Bone Shop Pin"),
                Config.Bind("Pins", "Unlock the Spa Pin", false, "Unlock Spa Pin"),
                Config.Bind("Pins", "Unlock the Stag Pin", false, "Unlock Stag Pin"),
                Config.Bind("Pins", "Unlock the Tube Pin", false, "Unlock Tube Pin"),
                Config.Bind("Pins", "Unlock the FleaMarrowlands Pin", false, "Unlock FleaMarrowlands Pin"),
                Config.Bind("Pins", "Unlock the FleaMidlands Pin", false, "Unlock FleaMidlands Pin"),
                Config.Bind("Pins", "Unlock the FleaBlastedlands Pin Nest", false, "Unlock Hunters FleaBlastedlands Pin"),
                Config.Bind("Pins", "Unlock the FleaCitadel Pin Steps", false, "Unlock Judge FleaCitadel Pin"),
                Config.Bind("Pins", "Unlock the FleaPeaklands Pin", false, "Unlock FleaPeaklands Pin"),
                Config.Bind("Pins", "Unlock the FleaMucklands Pin", false, "Unlock FleaMucklands Pin")
            }; 

            unlockAllMarkersAtStart = Config.Bind("Markers", "Unlock All Markers At Start", false, "Unlock All Markers At the Start");

            unlockAllMarkers = Config.Bind("Markers", "Unlock All Markers Now", false, "Unlock All Markers");

            // Config entries for each pin
            markerConfigs = new ConfigEntry<bool>[] {
                Config.Bind("Markers", "Unlock the hasMarker_a", false, "Unlock hasMarker_a"),
                Config.Bind("Markers", "Unlock the hasMarker_b", false, "Unlock hasMarker_b"),
                Config.Bind("Markers", "Unlock the hasMarker_c", false, "Unlock hasMarker_c"),
                Config.Bind("Markers", "Unlock the hasMarker_d", false, "Unlock hasMarker_d"),
                Config.Bind("Markers", "Unlock the hasMarker_e", false, "Unlock hasMarker_e")
            }; 

            // Add this line to explicitly create the config file
            Config.Save();

            resetDataAfterLeaving.SettingChanged += (sender, args) => {
                // stores mapData to reset to when leaving
                if (resetDataAfterLeaving.Value)
                {
                    resetManager.StoreOriginalData();
                }
            };

            hasQuill.SettingChanged += (sender, args) => plugin.changeQuillBool(hasQuill.Value);

            // apply 'unlock/lock all' to onClick/onConfigChanged functionalty.
            unlockAllMaps.SettingChanged += (sender, args) => OnConfigChangeAll(unlockAllMaps.Value, MapUnlocker.MAPS);
            unlockAllPins.SettingChanged += (sender, args) => OnConfigChangeAll(unlockAllPins.Value, MapUnlocker.PINS);
            unlockAllMarkers.SettingChanged += (sender, args) => OnConfigChangeAll(unlockAllMarkers.Value, MapUnlocker.MARKERS);

            // applies 'unlock/lock map' to onClick/onConfigChanged functionalty to all map configs.
            for (int map = 1; map < mapConfigs.Length; map++)
            {
                int currentMap = map;
                mapConfigs[currentMap].SettingChanged += (sender, args) =>
                {
                    OnConfigChange
                    (
                        MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS][currentMap],
                        mapConfigs[currentMap].Value,
                        ref allowChangeAllMaps,
                        ref isChangingAllMaps,
                        ref unlockAllMaps
                    );

                    if (!mapConfigs[currentMap].Value)
                    {
                        plugin.SetPlayerDataBool
                        (
                            MapUnlocker.playerDataFieldsBools[MapUnlocker.MAPS][MapUnlocker.ALL_FIELDS],
                            false
                        );
                    }
                };
            }

            // applies 'unlock/lock pin' to onClick/onConfigChanged functionalty to all pin configs.
            for (int pin = 0; pin < pinConfigs.Length; pin++)
            {
                int currentPin = pin;
                pinConfigs[currentPin].SettingChanged += (sender, args) => OnConfigChange
                (
                    MapUnlocker.playerDataFieldsBools[MapUnlocker.PINS][currentPin],
                    pinConfigs[currentPin].Value,
                    ref allowChangeAllPins,
                    ref isChangingAllPins,
                    ref unlockAllPins
                );
            }
            
            // applies 'unlock/lock pin' to onClick/onConfigChanged functionalty to all pin configs.
            for (int marker = 0; marker < markerConfigs.Length; marker++) 
            {
                int currentMarker = marker;
                markerConfigs[currentMarker].SettingChanged += (sender, args) =>
                {
                    OnConfigChange
                    (
                        MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS][currentMarker+1],
                        markerConfigs[currentMarker].Value,
                        ref allowChangeAllMarkers,
                        ref isChangingAllMarkers,
                        ref unlockAllMarkers
                    );

                    var hasMarkerField = MapUnlocker.GetPlayerDataBoolValue(MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS][0]);
                    if (hasMarkerField != null)
                    {
                        if (markerConfigs[currentMarker].Value && hasMarkerField == false)
                        {
                            plugin.SetPlayerDataBool
                            (
                                MapUnlocker.playerDataFieldsBools[MapUnlocker.MARKERS][0],
                                true
                            );
                        }
                    } else
                    {
                        Logger.LogError("hasMarkerField is null!");
                    }
                    
                };
            }

            if (debugMode!.Value) Logger.LogInfo($"Configuration initialized with {mapConfigs!.Length} map configs");
        }

        public void ChangeConfigData(ConfigEntry<bool>[] configBools, string[] dataFields)
        {
            if (PlayerData.instance != null)
            {
                for (int i = 0; i < dataFields.Length && i < configBools.Length; i++)
                {
                    // gets the map field from playerData
                    FieldInfo field = PlayerData.instance.GetType().GetField(dataFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // enables/disables config based on playerData 
                        configBools[i].Value = currentValue;
                        if (debugMode!.Value) Logger.LogInfo($"{dataFields[i]}: {configBools[i].Value}");

                    }
                }
            }
            else
            {
                if (debugMode!.Value) Logger.LogInfo("(PlayerData.instance is null");
            }
        }


        public void ChangeConfigData(ConfigEntry<bool>[] configBools, FieldInfo[] dataFields)
        {
            if (PlayerData.instance != null)
            {
                for (int i = 0; i < dataFields.Length && i < configBools.Length; i++)
                {
                    // gets the map field from playerData
                    FieldInfo field = dataFields[i];

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // enables/disables config based on playerData 
                        configBools[i].Value = currentValue;
                        if (debugMode!.Value) Logger.LogInfo($"{dataFields[i]}: {configBools[i].Value}");

                    }
                }
            }
            else
            {
                if (debugMode!.Value) Logger.LogInfo("(PlayerData.instance is null");
            }
        }
        

        /*
        * OnConfigChangedAllMaps: Unlocks or locks all maps within the mapFields list
        * value: the value to change map field values to. 
        */
        public void OnConfigChangeAll(bool value, int choice)
        {
            switch (choice)
            {
                default:
                case 0:
                    OnConfigChangeAll(value, mapConfigs, ref allowChangeAllMaps, ref isChangingAllMaps);
                    break;
                case 1:
                    OnConfigChangeAll(value, pinConfigs, ref allowChangeAllPins, ref isChangingAllPins);
                    break;
                case 2:
                    OnConfigChangeAll(value, markerConfigs, ref allowChangeAllMarkers, ref isChangingAllMarkers);
                    break;
            }
                
        }


        public void OnConfigChangeAll(bool value, ConfigEntry<bool>[] entries, ref bool allowChangeAll, ref bool isChangingAll)
        {
            if (debugMode!.Value) Logger.LogInfo("allowChangeAll: "+allowChangeAll);
            if (!allowChangeAll)
            {
                allowChangeAll = true;
                return;
            }

            if (PlayerData.instance != null)
            {
                isChangingAll = true;
                for (int entry = 0; entry < entries.Length; entry++)
                {
                    entries[entry].Value = value;
                }
            }
            else
            {
                if (debugMode!.Value) Logger.LogInfo("PlayerData could not be found");
            }

            isChangingAll = false;
        }



        private void OnConfigChange(string fieldName, bool value, ref bool allowChangeAll, ref bool isChangingAll, ref ConfigEntry<bool> unlockAllConfig)
        {
            // lock or unlocking string message purely for debugging purposes
            string action = value ? "Unlocked" : "Locked";

            if (PlayerData.instance != null)
            {
                // checks an logs if modifying the map data field was successful or not
                if (plugin.SetPlayerDataBool(PlayerData.instance, fieldName, value))
                {
                    // change unlockAllConfig entry to false without triggering its onSettingChanged function
                    if (!value)
                    {
                        if (debugMode!.Value) Logger.LogInfo("before isChangingAll: " + isChangingAll);

                        if (!isChangingAll) allowChangeAll = false;

                        if (debugMode.Value) Logger.LogInfo("after allowChangeAll: " + allowChangeAll);

                        unlockAllConfig.Value = value;
                    }

                }
                else
                {
                    if (debugMode!.Value) Logger.LogInfo(fieldName + " could not be " + action + "!");
                }
            }
        }
        
        private void OnConfigChange(FieldInfo fieldInfo, bool value, ref bool allowChangeAll, ref bool isChangingAll, ref ConfigEntry<bool> unlockAllConfig)
        {
            // lock or unlocking string message purely for debugging purposes
            string action = value ? "Unlocked" : "Locked";

            if (PlayerData.instance != null && fieldInfo != null)
            {
                // checks an logs if modifying the map data field was successful or not
                if (fieldInfo.FieldType == typeof(bool))
                {
                    if (plugin.SetPlayerDataBool(fieldInfo, value))
                    {
                        // change unlockAllConfig entry to false without triggering its onSettingChanged function
                        if (!value)
                        {
                            if (debugMode!.Value) Logger.LogInfo("before isChangingAll: " + isChangingAll);

                            if (!isChangingAll) allowChangeAll = false;
                            
                            if (debugMode.Value) Logger.LogInfo("after allowChangeAll: "+ allowChangeAll);
                            
                            unlockAllConfig.Value = value;
                        }
                    }
                    else
                    {
                        if (debugMode!.Value) Logger.LogInfo($"{fieldInfo.Name} could not be {action}!");
                    }
                }
            }
            else
            {
                if (debugMode!.Value) Logger.LogInfo($"FieldInfo is null or PlayerData.instance is null");
            }
        }
    }
}
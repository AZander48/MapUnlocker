using BepInEx.Configuration;
using UnityEngine;
using BepInEx;
using System.Reflection;
using System.Linq;
using GlobalSettings;
using UnityEngine.UIElements;

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
        public ConfigEntry<bool>? debugMode;
        public ConfigEntry<bool>? resetMapsAfterLeaving;
        public ConfigEntry<bool>? hasQuill;
        public ConfigEntry<bool>? unlockAllMapsAtStart;
        public ConfigEntry<bool>[]? mapConfigs;
        public ConfigEntry<bool>? unlockAllPinsAtStart;
        public ConfigEntry<bool>? unlockAllPins;
        public ConfigEntry<bool>[]? pinConfigs;

        // bool list to hold mapData for resets and saves 
        public bool[] originalMapData;
        public bool[] moddedMapData;

        private bool allowChangeAllMaps = true;
        private bool isChangingAllMaps = false;
        private bool allowChangeAllPins = true;
        private bool isChangingAllPins = false;

        /*
        * InitializeConfig: initializes the configuration menu binds and ui while applying onClick functionality.
        */
        public void InitializeConfig()
        {
            debugMode = Config.Bind("Debug", "Debug mode", false, "Enables log messages for debugging purposes.");
            resetMapsAfterLeaving = Config.Bind("General", "Reset Maps after leaving", false, "Resets all maps to original state after leaving.");
            hasQuill = Config.Bind("General", "Has Quill", false, "Enables the quill tool.");
            unlockAllMapsAtStart = Config.Bind("Maps", "Unlock All Maps At Start", false, "Unlock All Maps At the Start");

            // Config entries for each map
            mapConfigs = new ConfigEntry<bool>[] {
                Config.Bind("Maps", "Unlock All Maps Now", false, "Unlock All Maps"),
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

            // Add this line to explicitly create the config file
            Config.Save();

            resetMapsAfterLeaving.SettingChanged += (sender, args) => {
                // stores mapData to reset to when leaving
                if (resetMapsAfterLeaving.Value)
                {
                    resetManager.StoreOriginalData();
                }
            };

            hasQuill.SettingChanged += (sender, args) => plugin.changeQuillBool(hasQuill.Value);

            // apply 'unlock/lock all maps' to onClick/onConfigChanged functionalty to all maps config.
            mapConfigs[MapUnlocker.ALL_FIELDS].SettingChanged += (sender, args) => OnConfigChangedAllMaps(mapConfigs[MapUnlocker.ALL_FIELDS].Value);

            // apply 'unlock/lock all pin' to onClick/onConfigChanged functionalty to all maps config.
            unlockAllPins.SettingChanged += (sender, args) => OnConfigChangedAllPins(unlockAllPins.Value);

            // applies 'unlock/lock map' to onClick/onConfigChanged functionalty to all map configs.
            for (int map = 1; map < mapConfigs.Length; map++)
            {
                int currentMap = map;
                mapConfigs[currentMap].SettingChanged += (sender, args) => OnConfigChangedMap(MapUnlocker.mapFields[currentMap], mapConfigs[currentMap].Value);
            }
            
            // applies 'unlock/lock pin' to onClick/onConfigChanged functionalty to all pin configs.
            for (int pin = 0; pin < pinConfigs.Length; pin++) 
            {
                int currentPin = pin;
                pinConfigs[currentPin].SettingChanged += (sender, args) => OnConfigChangedPin(MapUnlocker.pinFields[currentPin], pinConfigs[currentPin].Value);
            }

            if (debugMode!.Value) Logger.LogInfo($"Configuration initialized with {mapConfigs!.Length} map configs");
        }

        /*
        * ChangeMapsConfigDataOnStart: Changes both config data to what maps are enabled already loading into a save.
        */
        public void ChangeMapsConfigDataOnStart()
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

                for (int i = 0; i < MapUnlocker.pinFields.Length && i < pinConfigs.Length; i++) 
                {
                    // gets the map field from playerData
                    FieldInfo field = PlayerData.instance.GetType().GetField(MapUnlocker.pinFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // enables/disables config based on playerData 
                        pinConfigs![i].Value = currentValue;
                        if (debugMode!.Value) Logger.LogInfo($"pinData {MapUnlocker.pinFields[i]}: {pinConfigs![i].Value}");

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
            if (!allowChangeAllMaps)
            {
                allowChangeAllMaps = true;
                return;
            }
            
            string action = value ? "Unlocked" : "Locked";

            if (PlayerData.instance != null)
            {
                isChangingAllMaps = true;

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

            isChangingAllMaps = false;
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
                    if (!value && mapConfigs![MapUnlocker.ALL_FIELDS].Value)
                    {
                        plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.mapFields[MapUnlocker.ALL_FIELDS], false);
                        if (!isChangingAllMaps) allowChangeAllMaps = false;
                        mapConfigs![MapUnlocker.ALL_FIELDS].Value = false;
                    }

                }
                else
                {
                    if (debugMode!.Value) Logger.LogInfo(mapName + " could not be " + action + "!");
                }
            }
        }
        

        /*
        * ChangePinConfigDataOnStart: .
        */
        public void ChangePinsConfigDataOnStart()
        {
            if (PlayerData.instance != null)
            {
                for (int i = 0; i < MapUnlocker.pinFields.Length && i < pinConfigs.Length; i++) 
                {
                    // gets the map field from playerData
                    FieldInfo field = PlayerData.instance.GetType().GetField(MapUnlocker.pinFields[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field != null && field.FieldType == typeof(bool))
                    {
                        bool currentValue = (bool)field.GetValue(PlayerData.instance);

                        // enables/disables config based on playerData 
                        pinConfigs![i].Value = currentValue;
                        if (debugMode!.Value) Logger.LogInfo($"pinData {MapUnlocker.pinFields[i]}: {pinConfigs![i].Value}");

                    }
                }
            } else {
                if (debugMode!.Value) Logger.LogInfo("(PlayerData.instance is null");
            }
        }


        public void OnConfigChangedAllPins(bool value)
        {
            if (!allowChangeAllPins)
            {
                allowChangeAllPins = true;
                return;
            }
            string action = value ? "Unlocked" : "Locked";

            if (PlayerData.instance != null)
            {
                isChangingAllPins = true;
                if (debugMode!.Value) Logger.LogInfo(pinConfigs.GetCount() + " pinConfigs!");
                if (debugMode!.Value) Logger.LogInfo(MapUnlocker.pinFields.GetCount() + " pinFields!");
                for (int pin = 0; pin < MapUnlocker.pinFields.Length; pin++)
                {
                    if (plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.pinFields[pin], value))
                    {
                        pinConfigs![pin].Value = value;
                        if (debugMode!.Value) Logger.LogInfo(MapUnlocker.pinFields[pin] + " has been " + action + "!");
                    }
                    else
                    {
                        if (debugMode!.Value) Logger.LogInfo(MapUnlocker.pinFields[pin] + " could not be " + action + "! Skipping map...");
                    }
                }
            }
            else
            {
                if (debugMode!.Value) Logger.LogInfo("PlayerData could not be found");
            }

            isChangingAllPins = false;
        }


        private void OnConfigChangedPin(string pinName, bool value)
        {
            // lock or unlocking string message purely for debugging purposes
            string action = value ? "Unlocked" : "Locked";

            if (PlayerData.instance != null)
            {
                // checks an logs if modifying the map data field was successful or not
                if (plugin.SetPlayerDataBool(PlayerData.instance, pinName, value))
                {
                    if (!value) {
                        plugin.SetPlayerDataBool(PlayerData.instance, MapUnlocker.pinFields[MapUnlocker.ALL_FIELDS], value);
                        if (!isChangingAllPins) allowChangeAllPins = false;
                        pinConfigs![MapUnlocker.ALL_FIELDS].Value = value;
                    }
                    
                } else {
                    if (debugMode!.Value) Logger.LogInfo(pinName+" could not be "+action+"!");
                }
            }
        }
    }
}
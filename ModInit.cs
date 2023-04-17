
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace ShellShuffler.Init
{


    public static class ModInit
    {
        internal static Logger modLog;
        internal static string modDir;

        public static ShellShufflerSettings modSettings = new ShellShufflerSettings();
        public const string HarmonyPackage = "us.tbone.ShellShuffler";
        public static void Init(string directory, string settingsJSON)
        {
            modDir = directory;
            try
            {
                ModInit.modSettings = JsonConvert.DeserializeObject<ShellShufflerSettings>(settingsJSON);
            }
            catch (Exception)
            {
                
                ModInit.modSettings = new ShellShufflerSettings();
            }
            modLog = new Logger(modDir, "ShellShuffler", modSettings.enableLogging);
            //var harmony = HarmonyInstance.Create(HarmonyPackage);
            //harmony.PatchAll(Assembly.GetExecutingAssembly());
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), HarmonyPackage);
        }

    }
    public class ShellShufflerSettings
    {
        public bool enableLogging = false;

        public bool shuffleMechs = false;
        public bool shuffleVehicles = false;
        public bool shuffleTurrets = false;
        public bool tagSetsUnion = false;
        public float chanceToShuffle = 1f;
        public int unShuffledBins = 1;

        public List<string> blackListShuffleIn = new List<string>();
        public List<string> blackListShuffleOut = new List<string>();

        public Dictionary<string, List<string>> mechDefTagAmmoList = new Dictionary<string, List<string>>();

        public Dictionary<string, int> ammoWeight = new Dictionary<string, int>();

        public Dictionary<string, List<string>> factionAmmoList = new Dictionary<string, List<string>>();

    }
}
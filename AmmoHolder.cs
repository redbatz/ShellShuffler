using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ShellShuffler.Init;

namespace ShellShuffler
{
    public class AmmoHolder
    {
        private static AmmoHolder _instance;
        public DataManager dataManager;
        public Dictionary<string, AmmunitionDef> ammoById = new ();
        public Dictionary<AmmoCategoryValue, List<AmmunitionBoxDef>> ammoBoxesByCategory = new();

        public static AmmoHolder AmmoHolderInstance
        {
            get
            {
                if (_instance == null) _instance = new AmmoHolder();
                return _instance;
            }
        }

        internal void Initialize(DataManager dm)
        {
            dataManager = dm;
            ammoBoxesByCategory = new Dictionary<AmmoCategoryValue, List<AmmunitionBoxDef>>();
            ammoById = new Dictionary<string, AmmunitionDef>();
            foreach (var ammo in dataManager.AmmoDefs.Where(x => x.Value.Description != null).Select(x => x.Value))
            {
                if (ammo.AmmoCategoryValue.UsesInternalAmmo)
                {
                    continue;
                }
                ammoById.Add(ammo.Description.Id, ammo);
                ModInit.modLog.LogMessage($"Added Ammo Def: {ammo.Description.Name} to ammoList");
            }

            foreach (AmmunitionBoxDef ammunitionBoxDef in dataManager.AmmoBoxDefs.Where(x => x.Value.Description != null).Select(x => x.Value))
            {
                if (ammunitionBoxDef.Description.Id.EndsWith("ContentAmmunitionBoxDef"))
                {
                    ModInit.modLog.LogMessage($"Ammo Box Def: {ammunitionBoxDef.Description.Name} was ContentAmmunitionBoxDef, skipping.");
                    continue;
                }

                if (ammunitionBoxDef.ComponentTags != null && ammunitionBoxDef.ComponentTags.ContainsAny(ModInit.modSettings.BlacklistAmmoboxOutTags))
                {
                    ModInit.modLog.LogMessage($"Skipping Ammo Box Def: {ammunitionBoxDef.Description.Name} due to blacklisted tags");
                    continue;
                }

                if (!ammoById.TryGetValue(ammunitionBoxDef.AmmoID, out var ammoDef))
                {
                    ModInit.modLog.LogMessage($"Skipping Ammo Box Def: Unable to find ammunition {ammunitionBoxDef.AmmoID} for {ammunitionBoxDef.Description.Id}");
                    continue;
                }

                if (!ammoBoxesByCategory.TryGetValue(ammoDef.AmmoCategoryValue, out var list) || list == null)
                {
                    list = new List<AmmunitionBoxDef>();
                    ammoBoxesByCategory[ammoDef.AmmoCategoryValue] = list;
                }
                list.Add(ammunitionBoxDef);

                ModInit.modLog.LogMessage($"Added Ammo Box Def: {ammunitionBoxDef.Description.Name} to ammoBoxList with ammo category {ammoDef.AmmoCategoryValue?.Name}, weight {ammunitionBoxDef.Tonnage}t and capacity {ammunitionBoxDef.Capacity}");
            }
        }
    }
}
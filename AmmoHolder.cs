using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.Data;
using ShellShuffler.Init;

namespace ShellShuffler
{
    public class AmmoHolder
    {
        private static AmmoHolder _instance;
        public DataManager dataManager;
        public List <AmmunitionDef> ammoList = new List<AmmunitionDef>();
        public List <AmmunitionBoxDef> ammoBoxList = new List<AmmunitionBoxDef>();

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
            ammoBoxList = new List<AmmunitionBoxDef>();
            foreach (var t in dataManager.AmmoBoxDefs)
            {
                if (!t.Value.Description.Id.EndsWith("ContentAmmunitionBoxDef"))
                {
                    ammoBoxList.Add(t.Value);
                    ModInit.modLog.LogMessage($"Added Ammo Box Def: {t.Value.Description.Name} to ammoBoxList with capacity {t.Value.Capacity}");
                }
                else
                {
                    ModInit.modLog.LogMessage($"Ammo Box Def: {t.Value.Description.Name} was ContentAmmunitionBoxDef, skipping.");
                }
            }
            ammoList = new List<AmmunitionDef>();
            foreach (var t in dataManager.AmmoDefs)
            {
                ammoList.Add(t.Value);
                ModInit.modLog.LogMessage($"Added Ammo Def: {t.Value.Description.Name} to ammoList");

            }
        }
    }
}
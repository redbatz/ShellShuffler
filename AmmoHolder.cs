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
        public List <AmmunitionDef> ammoList;
        public List <AmmunitionBoxDef> ammoBoxList;

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
                ammoBoxList.Add(t.Value);
                ModInit.modLog.LogMessage($"Added Ammo Box Def: {t.Value.Description.Name} to ammoBoxList");
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
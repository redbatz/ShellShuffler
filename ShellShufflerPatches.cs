using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using UnityEngine;
using ShellShuffler.Init;

namespace ShellShuffler.Patches
{
    class ShellShufflerPatches
    {
        [HarmonyPatch(typeof(CombatGameState), "_Init",
            new Type[] { typeof(GameInstance), typeof(Contract), typeof(string) })]
        public static class CGS__Init_patch
        {
            public static void Postfix(CombatGameState __instance, GameInstance game, Contract contract,
                string localPlayerTeamGuid)
            {
                AmmoHolder.AmmoHolderInstance.Initialize(game.DataManager);
                ModInit.modLog.LogMessage($"Initialized AmmoHolder");
            }
        }

        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] { typeof(AbstractActor) })]
        public static class Team_AddUnit
        {
            //private static MethodInfo assignAmmo = AccessTools.Method(typeof(AbstractActor), "AssignAmmoToWeapons");
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (unit.team.IsLocalPlayer) return;
                if (unit.GetStaticUnitTags().Any(x => ModInit.modSettings.unitBlackList.Contains(x)))
                {
                    ModInit.modLog.LogMessage(
                        $"{unit.Description.Name} has blacklisted tag from unitBlackList; not shuffling!");
                    return;
                }

                if (unit is Mech && !ModInit.modSettings.shuffleMechs)
                {
                    ModInit.modLog.LogMessage(
                        $"{unit.Description.Name} is Mech and shuffleMechs = false; not shuffling!");
                    return;
                }

                if (unit is Vehicle && !ModInit.modSettings.shuffleVehicles)
                {
                    ModInit.modLog.LogMessage(
                        $"{unit.Description.Name} is Vehicle and shuffleVehicles = false; not shuffling!");
                    return;
                }

                if (unit is Turret && !ModInit.modSettings.shuffleTurrets)
                {
                    ModInit.modLog.LogMessage(
                        $"{unit.Description.Name} is Turret and shuffleTurrets = false; not shuffling!");
                    return;
                }

                var rand = new System.Random();
                var roll = rand.NextDouble();

                var chance = Mathf.Clamp(ModInit.modSettings.chanceToShuffle, 0f, 1f);
                if (roll > chance)
                {
                    ModInit.modLog.LogMessage($"Roll of {roll} >= {chance}; not shuffling!");
                    return;
                }
                //adding 'running total' of bins?

                var multiBins = unit.ammoBoxes.GroupBy(x => x.ammoDef.AmmoCategoryValue);

                var shuffleBins = new List<AmmunitionBox>();
                foreach (var cat in multiBins)
                {
                    if (cat.Count() > ModInit.modSettings.unShuffledBins)
                    {
                        foreach (var bin in cat.Skip(ModInit.modSettings.unShuffledBins))
                        {
                            if (bin.ammunitionBoxDef == null)
                            {
                                continue;
                            }

                            if ((bin.ammunitionBoxDef.ComponentTags != null) && (bin.ammunitionBoxDef.ComponentTags.ContainsAny(ModInit.modSettings.BlacklistAmmoboxInTags)))
                            {
                                ModInit.modLog.LogMessage($"{bin.Description.Name}/{bin.Description.UIName}/{bin.defId} can't be shuffled!");
                                continue;
                            }

                            shuffleBins.Add(bin);
                            ModInit.modLog.LogMessage($"{bin.Description.Name}/{bin.Description.UIName} can be shuffled!");
                        }
                    }
                }

                //foreach (var t1 in new List<MechComponent>(unit.allComponents))
                foreach (var t1 in new List<MechComponent>(shuffleBins))
                {
                    if (t1.componentType == ComponentType.AmmunitionBox)
                    {
                        AmmunitionBox ab = t1 as AmmunitionBox;
                        if (ab == null) return;
                        //if ammo in blacklist, don't do anything
                        if (ab != null && ModInit.modSettings.blackListShuffleOut.Contains(ab.ammoDef.Description.Id))
                        {
                            ModInit.modLog.LogMessage(
                                $"Original ammo {ab.ammoDef.Description.Name} is in blacklist, not shuffling!");
                            return;
                        }

                        if (ab != null)
                        {
                            ModInit.modLog.LogMessage(
                                $"Original Ammo Box: {ab.Description.Name}/{ab.Description.UIName}, Tonnage: {ab.tonnage}");

                            HashSet<AmmunitionDef> alternateAmmunitions = new HashSet<AmmunitionDef>();

                            var alternateBoxDefsList = AmmoHolder.AmmoHolderInstance.ammoBoxesByCategory[ab.ammoCategoryValue];

                            if (alternateBoxDefsList == null || !alternateBoxDefsList.Any())
                            {
                                ModInit.modLog.LogMessage($"No ammunition boxes found for category {ab.ammoCategoryValue}!");
                                return;
                            }

                            ModInit.modLog.LogMessage($"Filtering potential ammo boxes and types by tonnage!");
                            alternateBoxDefsList = alternateBoxDefsList.Where(x => Math.Abs(x.Tonnage - ab.tonnage) < 0.01f).ToList();
                            
                            foreach (var a in alternateBoxDefsList)
                            {
                                ModInit.modLog.LogMessage(
                                    $"Possible ammo boxes: {a.Description.Name}/{a.Description.UIName} {a.Tonnage}t");
                            }

                            alternateAmmunitions = alternateBoxDefsList.Select(alt => AmmoHolder.AmmoHolderInstance.ammoById[alt.AmmoID]).ToHashSet();

                            foreach (var a in alternateAmmunitions)
                            {
                                ModInit.modLog.LogMessage(
                                    $"Tonnage and category matches found: {a.Description.Name}/{a.Description.UIName}");
                            }

                            string ammolog = string.Empty;

                            if (ModInit.modSettings.mechDefTagAmmoList.Any())
                            {
                                var unitTags = unit.GetTags();

                                var tagKeys =
                                    new List<string>(
                                        ModInit.modSettings.mechDefTagAmmoList.Keys.Where(x => unitTags.Contains(x)));

                                var tagVals = new List<string>();

                                if (tagKeys.Any())
                                {
                                    List<string> inspectKeys = (List<string>)tagKeys;
                                    ModInit.modLog.LogMessage($"potential tagkeys: {string.Join(" | ", tagKeys)}");

                                    foreach (var tag in tagKeys)
                                    {
                                        //tagVals.AddRange(tagKeys.Intersect(ModInit.modSettings.mechDefTagAmmoList[tag]));
                                        tagVals.AddRange(ModInit.modSettings.mechDefTagAmmoList[tag]);
                                    }

                                    if (!ModInit.modSettings.tagSetsUnion)
                                    {
                                        var taggedAmmo = tagVals.GroupBy(x => x)
                                            .Select(g => new { Value = g.Key, Count = g.Count() });

                                        var intersectedAmmo =
                                            taggedAmmo.Where(x => x.Count == taggedAmmo.Max(g => g.Count));

                                        var finalAmmo = new List<string>();
                                        foreach (var ammo in intersectedAmmo)
                                        {
                                            finalAmmo.Add(ammo.Value);
                                        }

                                        tagVals = finalAmmo;
                                    }

                                    tagVals = tagVals.Distinct().ToList();

                                    if (tagVals.Any())
                                    {
                                        ModInit.modLog.LogMessage($"Removing ammos per unitDefTags");
                                        alternateAmmunitions.RemoveWhere(x => !tagVals.Contains(x.Description.Id));
                                        ammolog = string.Join(" | ", alternateAmmunitions.Select(x => x.Description.Id).ToList());
                                        ModInit.modLog.LogMessage(
                                            $"Remaining valid ammos from tags for {unit.Description.Name}: {ammolog}");
                                    }
                                }
                            }

                            if (ModInit.modSettings.factionAmmoList.Any())
                            {
                                var unitFID = __instance?.FactionValue?.Name; //Traverse.Create(__instance).Field("factionID").GetValue<string>();
                                if (!string.IsNullOrEmpty(unitFID))
                                {
                                    if (ModInit.modSettings.factionAmmoList.ContainsKey(unitFID))
                                    {
                                        List<string> factionAmmos = ModInit.modSettings.factionAmmoList[unitFID];

                                        alternateAmmunitions.RemoveWhere(x => !factionAmmos.Contains(x.Description.Id));
                                        ammolog = string.Join(" | ", alternateAmmunitions.Select(x => x.Description.Id).ToList());
                                        ModInit.modLog.LogMessage(
                                            $"Remaining valid ammos from faction list for {unit.Description.Name}: {ammolog}");
                                    }
                                }
                            }

                            //remove blacklisted ammo from shuffle pool
                            alternateAmmunitions.RemoveWhere(x => ModInit.modSettings.blackListShuffleIn.Contains(x.Description.Id));
                            ModInit.modLog.LogMessage($"Removing all blacklisted ammos from pool.");

                            // Remove restricted ammunition based on unit tags 
                            Dictionary<string, string> removeDueToTagRestriction = new Dictionary<string, string>();
                            foreach (string key in ModInit.modSettings.mechDefTagRestrictedAmmoList.Keys)
                            {
                                if (unit.GetTags().Contains(key))
                                {
                                    var restrictedAmmo = ModInit.modSettings.mechDefTagRestrictedAmmoList[key];
                                    foreach (var val in restrictedAmmo)
                                    {
                                        removeDueToTagRestriction.Add(val, key);
                                    }
                                }
                            }

                            alternateAmmunitions.RemoveWhere(ammo =>
                            {
                                if (removeDueToTagRestriction.TryGetValue(ammo.Description.Id, out var tag))
                                {
                                    ModInit.modLog.LogMessage($"Match on unit tag {tag}: Removing restricted ammunition {ammo.Description.Id}");
                                    return true;
                                }

                                return false;
                            });

                            ammolog = string.Join(" | ", alternateAmmunitions.Select(x => x.Description.Id).ToList());
                            ModInit.modLog.LogMessage(
                                $"Remaining valid ammos for {unit.Description.Name}: {ammolog}");

                            if (!alternateAmmunitions.Contains(ab.ammoDef))
                            {
                                alternateAmmunitions.Add(ab.ammoDef); //make sure original is still in list
                                ModInit.modLog.LogMessage(
                                    $"Original {ab.ammoDef.Description.Name}/{ab.ammoDef.Description.UIName} not in list due to filter, adding it back.");
                            }

                            foreach (var ammodef in new List<AmmunitionDef>(alternateAmmunitions))
                            {
                                if (ModInit.modSettings.ammoWeight.ContainsKey(ammodef.Description.Id))
                                {
                                    for (int i = 0; i < ModInit.modSettings.ammoWeight[ammodef.Description.Id]; i++)
                                    {
                                        alternateAmmunitions.Add(ammodef);
                                    }
                                }
                            }

                            Dictionary<AmmunitionBoxDef, AmmunitionDef> possibleSelections = new Dictionary<AmmunitionBoxDef, AmmunitionDef>();
                            var ammunitionLookup = alternateAmmunitions.ToDictionary(a => a.Description.Id, a => a);
                            foreach (var ammunitionBoxDef in alternateBoxDefsList)
                            {
                                if (ammunitionLookup.TryGetValue(ammunitionBoxDef.AmmoID, out var matchedAmmunition))
                                {
                                    ModInit.modLog.LogMessage($"Adding {ammunitionBoxDef.Description.Id} ({ammunitionBoxDef.Tonnage}t) of ammunition {matchedAmmunition.Description.Id} to possible selections");
                                    possibleSelections.Add(ammunitionBoxDef, matchedAmmunition);
                                }
                            }

                            if (possibleSelections.Count == 1)
                            {
                                ModInit.modLog.LogMessage("No alternative ammunition boxes remain, abort shuffling");
                                return;
                            }

                            var idx = UnityEngine.Random.Range(0, possibleSelections.Count);
                            var selection = possibleSelections.ElementAt(idx);
                            ModInit.modLog.LogMessage($"Replacement Box Chosen: {selection.Key.Description.Name}/{selection.Key.Description.UIName} of ammunition {selection.Value.Description.Name}/{selection.Value.Description.UIName}");

                            var alternateBoxDef = selection.Key;

                            alternateBoxDef.refreshAmmo(AmmoHolder.AmmoHolderInstance.dataManager);
                            ModInit.modLog.LogMessage(
                                $"Swapping {ab.Description.Name}/{ab.Description.UIName} for {alternateBoxDef?.Description?.Name}/{alternateBoxDef?.Description?.UIName}.");

                            //    var sim = UnityGameInstance.BattleTechGame.Simulation;

                            if (unit is Mech)
                            {
                                var mech = unit as Mech;
                                var mref = new MechComponentRef(alternateBoxDef.Description.Id,

                                    //sim.GenerateSimGameUID(),
                                    t1.uid, //new 111020
                                    ComponentType.AmmunitionBox,
                                    t1.LocationDef.Location, -1, ComponentDamageLevel.Functional, false);


                                mref.Def = alternateBoxDef; //Traverse.Create(mref).Property("Def").SetValue(alternateBoxDef);
                                mref.RefreshComponentDef();

                                AmmunitionBox repAB = new AmmunitionBox(mech, mref, 0.ToString());
                                repAB.InitStats();
                                //                                repAB.FromAmmunitionBoxRef(mref);

                                mech.ammoBoxes.Remove(ab);
                                mech.ammoBoxes.Add(repAB);
                                mech.allComponents.Remove(ab);
                                mech.allComponents.Add(repAB);

                                t1.FromMechComponentRef(mref);
                            }

                            if (unit is Vehicle)
                            {
                                var vic = unit as Vehicle;
                                var vref = new VehicleComponentRef(alternateBoxDef.Description.Id,

                                    //sim.GenerateSimGameUID(),
                                    t1.uid, //new 111020
                                    ComponentType.AmmunitionBox,
                                    t1.VehicleLocationDef.Location, -1, ComponentDamageLevel.Functional);


                                vref.Def = alternateBoxDef; //Traverse.Create(vref).Property("Def").SetValue(alternateBoxDef);
                                vref.RefreshComponentDef();

                                AmmunitionBox repAB = new AmmunitionBox(vic, vref, 0.ToString());
                                repAB.InitStats();

                                vic.ammoBoxes.Remove(ab);
                                vic.ammoBoxes.Add(repAB);
                                vic.allComponents.Remove(ab);
                                vic.allComponents.Add(repAB);

                                t1.FromVehicleComponentRef(vref);
                            }

                            if (unit is Turret)
                            {
                                var trt = unit as Turret;
                                var tref = new TurretComponentRef(alternateBoxDef.Description.Id,

                                    //sim.GenerateSimGameUID(),
                                    t1.uid, //new 111020
                                    ComponentType.AmmunitionBox,
                                    t1.VehicleLocationDef.Location, -1, ComponentDamageLevel.Functional);


                                tref.Def = alternateBoxDef;
                                tref.RefreshComponentDef();

                                AmmunitionBox repAB = new AmmunitionBox(trt, tref, 0.ToString());
                                repAB.InitStats();

                                trt.ammoBoxes.Remove(ab);
                                trt.ammoBoxes.Add(repAB);
                                trt.allComponents.Remove(ab);
                                trt.allComponents.Add(repAB);
                            }
                        }
                    }
                }

                unit.AssignAmmoToWeapons();
                //assignAmmo.Invoke(unit, new object[]{});
                //Traverse.Create(unit).Method("AssignAmmoToWeapons").GetValue();
            }
        }
    }
}
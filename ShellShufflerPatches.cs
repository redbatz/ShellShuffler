using System;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using UnityEngine;
using ShellShuffler;
using BattleTech.Data;
using ShellShuffler.Init;

namespace ShellShuffler.Patches
{
    class ShellShufflerPatches
    {
        [HarmonyPatch(typeof(CombatGameState), "_Init", new Type[] {typeof(GameInstance), typeof(Contract), typeof(string)})]
        public static class CGS__Init_patch
        {
            public static void Postfix(SimGameState __instance, GameInstance game, Contract contract, string localPlayerTeamGuid)
            {
                AmmoHolder.AmmoHolderInstance.Initialize(game.DataManager);
                ModInit.modLog.LogMessage($"Initialized AmmoHolder");
            }
        }

        [HarmonyPatch(typeof(Team), "AddUnit", new Type[] {typeof(AbstractActor)})]
        public static class Team_AddUnit
        {
            public static void Postfix(Team __instance, AbstractActor unit)
            {
                if (unit.team.IsLocalPlayer) return;

                if (unit is Mech && !ModInit.modSettings.shuffleMechs)
                {
                    ModInit.modLog.LogMessage($"{unit.Description.Name} is Mech and shuffleMechs = false; not shuffling!");
                    return;
                }

                if (unit is Vehicle && !ModInit.modSettings.shuffleVehicles)
                {
                    ModInit.modLog.LogMessage($"{unit.Description.Name} is Vehicle and shuffleVehicles = false; not shuffling!");
                    return;
                }

                if (unit is Turret && !ModInit.modSettings.shuffleTurrets)
                {
                    ModInit.modLog.LogMessage($"{unit.Description.Name} is Turret and shuffleTurrets = false; not shuffling!");
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


                foreach (var t1 in unit.allComponents)
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

                            var alternateDefsList = new List<AmmunitionDef>();

                            var alternateBoxDefsList = new List<AmmunitionBoxDef>();


                            foreach (var alt in AmmoHolder.AmmoHolderInstance.ammoBoxList)
                            {
                                if (Math.Abs(alt.Tonnage - ab.tonnage) < 0.01)
                                {
                                    alternateBoxDefsList.Add(alt);
                                    alternateDefsList.AddRange(AmmoHolder.AmmoHolderInstance.ammoList.Where(x =>
                                        x?.Description?.Id == alt?.AmmoID && (Equals(x?.AmmoCategoryValue,
                                            ab.ammoDef.AmmoCategoryValue))));

                                }
                            }
                            ModInit.modLog.LogMessage($"Filtering potential ammo boxes and types by tonnage!");
                            

                            string ammolog = string.Empty;

                            if (ModInit.modSettings.mechDefTagAmmoList.Any())
                            {
                                var unitTags = unit.GetTags();

                                var tagKeys = new List<string>(ModInit.modSettings.mechDefTagAmmoList.Keys.Where(x => unitTags.Contains(x)));

                                var tagVals = new List<string>();

                                if (tagKeys.Any())
                                {
                                    List<string> inspectKeys = (List<string>)tagKeys;
                                    ModInit.modLog.LogMessage($"potential tagkeys: {string.Join("|",tagKeys)}");
                                    
                                    foreach (var tag in tagKeys)
                                    {
                                        //tagVals.AddRange(tagKeys.Intersect(ModInit.modSettings.mechDefTagAmmoList[tag]));
                                        tagVals.AddRange(ModInit.modSettings.mechDefTagAmmoList[tag]);
                                    }

                                    if (!ModInit.modSettings.tagSetsUnion)
                                    {
                                        var taggedAmmo = tagVals.GroupBy(x => x)
                                            .Select(g => new { Value = g.Key, Count = g.Count() });

                                        var intersectedAmmo = taggedAmmo.Where(x => x.Count == taggedAmmo.Max(g => g.Count));

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
                                        alternateDefsList.RemoveAll(x => !tagVals.Contains(x.Description.Id));
                                        ammolog = string.Join("|", alternateDefsList);
                                        ModInit.modLog.LogMessage(
                                            $"Remaining valid ammos from tags for {unit.Description.Name}: {ammolog}");
                                    }
                                }

                            }

                            if (ModInit.modSettings.factionAmmoList.Any())
                            {
                                var unitFID = Traverse.Create(__instance).Field("factionID").GetValue<string>();

                                if (ModInit.modSettings.factionAmmoList.ContainsKey(unitFID))
                                {
                                    List<string> factionAmmos = ModInit.modSettings.factionAmmoList[unitFID];

                                    alternateDefsList.RemoveAll(x => !factionAmmos.Contains(x.Description.Id));
                                    ammolog = string.Join("|", alternateDefsList);
                                    ModInit.modLog.LogMessage(
                                        $"Remaining valid ammos from faction list for {unit.Description.Name}: {ammolog}");
                                }
                            }


                            //remove blacklisted ammo from shuffle pool
                            alternateDefsList.RemoveAll(x =>
                                ModInit.modSettings.blackListShuffleIn.Contains(x.Description.Id));
                            ModInit.modLog.LogMessage(
                                $"Removing all blacklisted ammos from pool.");

                            ammolog = string.Join("|", alternateDefsList);
                            ModInit.modLog.LogMessage(
                                $"Remaining valid ammos for {unit.Description.Name}: {ammolog}");

                            if (!alternateDefsList.Contains(ab.ammoDef))
                            {
                                alternateDefsList.Add(ab.ammoDef); //make sure original is still in list
                                ModInit.modLog.LogMessage(
                                    $"Original {ab.ammoDef.Description.Name}/{ab.ammoDef.Description.UIName} not in list due to filter, adding it back.");
                            }

                            foreach (var ammodef in new List<AmmunitionDef>(alternateDefsList))
                            {
                                if (ModInit.modSettings.ammoWeight.ContainsKey(ammodef.Description.Id))
                                {
                                    for (int i = 0; i < ModInit.modSettings.ammoWeight[ammodef.Description.Id]; i++)
                                    {
                                        alternateDefsList.Add(ammodef);
                                    }
                                }
                            }
                            

                            ReChoose:

                            var idx = UnityEngine.Random.Range(0, alternateDefsList.Count());
                            var alternateDef = alternateDefsList[idx];
                            ModInit.modLog.LogMessage(
                                $"Replacement Ammo Chosen: {alternateDef.Description.Name}/{alternateDef?.Description?.UIName}");

                            var alternateBoxDef = alternateBoxDefsList.FirstOrDefault(x =>
                                x?.AmmoID == alternateDef?.Description?.Id);


                            ModInit.modLog.LogMessage(
                                $"Found potential ammo boxes for {alternateDef?.Description?.Name}/{alternateDef?.Description?.UIName}: {alternateBoxDef?.Description?.UIName}");

                            if (alternateBoxDef == null)
                            {
                                ModInit.modLog.LogMessage($"Something borked, trying again.");
                                goto ReChoose;
                            }

                            ModInit.modLog.LogMessage(
                                $"Tonnage match found, swapping {ab.Description.Name}/{ab.Description.UIName} for {alternateDef.Description.Name}/{alternateDef.Description.UIName}.");
                            alternateBoxDef.refreshAmmo(AmmoHolder.AmmoHolderInstance.dataManager);

                            //////////////////this was added recently!!!
                            var sim = UnityGameInstance.BattleTechGame.Simulation;


                            var mref = new MechComponentRef(alternateBoxDef.Description.Id,
                                sim.GenerateSimGameUID(), alternateBoxDef.ComponentType,
                                t1.LocationDef.Location, -1, ComponentDamageLevel.Functional, false);

                            Traverse.Create(t1).Property("componentDef").SetValue(alternateBoxDef);
                            Traverse.Create(t1).Property("mechComponentRef").SetValue(mref);
                            ab.InitStats();
                        }
                    }
                }
            }
        }
    }
}
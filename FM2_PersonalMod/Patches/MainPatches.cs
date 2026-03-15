using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Diagnostics;
using static UnityEngine.UI.CanvasScaler;

namespace FM2_PersonalMod.Patches
{
    [HarmonyPatch]
    internal class MainPatches
    {
        private static readonly int SkillLimit = 20;

        [HarmonyPatch(typeof(UnitsScaleChanger), "SetUnitScale")]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static void ScaleMultiply(CombatStateManager combatController, BattleUnit unit)
        {
            Unit.UnitType type = unit.type;
            Vector3 localScale = unit.gameObject.transform.localScale;
            UnitsScaleDataBase unitsScaleDataBase = combatController.UnitsScaleDataBase;      
            float d = unitsScaleDataBase.GetScaleMultiplierByType(type);
            if (unit.BattleScaleOverride != null)
            {
                d = unit.BattleScaleOverride.ScaleMultiplier;
            }
            if (Utils.Helpers.IsPlayer(unit))
            {
                
                if (Utils.Helpers.SpecialPilots.Contains(unit.Pilot.Stats.Callsign))
                {
                    d *= (float)1.05;
                }
                else if (Utils.Helpers.CommanderPilots.Contains(unit.Pilot.Stats.Callsign))
                {
                    d *= (float)1.05;
                }
                FM2_PersonalModPlugin.Log.LogInfo($"Player Unit Scale Modifier: {d}");
            }
            unitsScaleDataBase.SaveScale(unit, localScale);
            unit.gameObject.transform.localScale *= d;
            FM2_PersonalModPlugin.Log.LogInfo($"Unit Team: {unit.unitTeam}, Unit Scale: {unit.gameObject.transform.localScale}");

        }

        [HarmonyPatch(typeof(SkillProcessor), "ChooseSkill")]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static void SkillChoice(ref SkillPreprocessingData data, AttackInstance attack, bool isAttacker)
        {
            BattleUnit attackerUnit = attack.AttackerUnit;
            BattleUnit defenderUnit = attack.DefenderUnit;

            if (data.FirstSkill != true)
            {
                return;
            }       
            if (attackerUnit is PlayerUnit && isAttacker && data.AllowedSkills == Utils.Helpers.DefaultChainCount)
            {
                int limit = Utils.Helpers.GetChainLimit(attackerUnit);
                data.AllowedSkills = limit;
            }
            if (defenderUnit is PlayerUnit && !isAttacker && data.AllowedSkills == Utils.Helpers.DefaultChainCount)
            {
                int limit = Utils.Helpers.GetChainLimit(defenderUnit);
                data.AllowedSkills = limit;
            }

        }

        [HarmonyPatch(typeof(BattleSkillBase), nameof(BattleSkillBase.GetActivationChance))]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ActivationChance(BattleSkillBase __instance, ref float __result, bool isAttacker, AttackInstance attackInstance)
        {
            BattleUnit attackerUnit = attackInstance.AttackerUnit;
            BattleUnit defenderUnit = attackInstance.DefenderUnit;
            double multiplier = 1;
            FM2_PersonalModPlugin.Log.LogInfo($"Original Skill Name: {__instance.SkillName}, Activation Chance: {__result}");

            if (attackerUnit is PlayerUnit && isAttacker)
            {
                multiplier *= 1.5;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 2;
                }
                else if (Utils.Helpers.CommanderPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 1.5;
                }
            }
            if (defenderUnit is PlayerUnit && !isAttacker)
            {
                multiplier *= (float)1.5;
                if (Utils.Helpers.SpecialPilots.Contains(defenderUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 2;
                }
                else if (Utils.Helpers.CommanderPilots.Contains(defenderUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 1.5;
                }
            }
            __result *= (float)multiplier;
            FM2_PersonalModPlugin.Log.LogInfo($"Skill Trigger: {__instance.SkillName}, Activation Chance: {__result}, Multiplier: {multiplier}");

        }

        [HarmonyPatch(typeof(BattleSkillBase), "GetPreprocessingData")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ModifyAffinity(BattleSkillBase __instance, AttackInstance attackInstance, bool isAttacker, ref SkillPreprocessingData __result)
        {
            BattleUnit attackerUnit = attackInstance.AttackerUnit;
            BattleUnit defenderUnit = attackInstance.DefenderUnit;
            if (attackerUnit is PlayerUnit && isAttacker)
            {
                if (!__instance.HasAfinity)
                {
                    __result.AllowedSkills = 4;
                }
            }
            if (defenderUnit is PlayerUnit && !isAttacker)
            {
                if (!__instance.HasAfinity)
                {
                    __result.AllowedSkills = 4;
                }
            }
        }


        [HarmonyPatch(typeof(BattleSkillBase), "GetChainProbability")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ChainChance(BattleSkillBase __instance, ref float __result, AttackInstance attackInstance, bool isAttacker, SkillPreprocessingData preprocessing)
        {
            double multiplier = 1;
            BattleUnit attackerUnit = attackInstance.AttackerUnit;
            BattleUnit defenderUnit = attackInstance.DefenderUnit;
            if (attackerUnit is PlayerUnit && isAttacker)
            {
                multiplier *= 1.3;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 1.2;

                }
                else if (Utils.Helpers.CommanderPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 1.1;
                }
            }
            if (defenderUnit is PlayerUnit && !isAttacker)
            {
                multiplier *= 1.5;
                if (Utils.Helpers.SpecialPilots.Contains(defenderUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 1.5;
                }
            }
            __result *= (float)multiplier;
            FM2_PersonalModPlugin.Log.LogInfo($"Skill Chain: {__instance.SkillName}, Chance: {__result}, Allowed Skills: {preprocessing.AllowedSkills}, Chance Multiplier: {multiplier}");
        }

        [HarmonyPatch(typeof(UnitGraphics), "LoadParts")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ReloadParts(UnitGraphics __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (__instance.Unit == null)
            {
                return;
            }
            if (!Utils.Helpers.IsPlayer(__instance.Unit))
            {
                return;
            }
            Utils.Helpers.UpdatePlayerSkills(__instance.Pilot.Stats);

            FM2_PersonalModPlugin.Log.LogInfo($"Successfully Reloaded Skills! (LoadParts)");
        }

        [HarmonyPatch(typeof(UnitGraphics), "ReloadStartingStats")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ReloadStats(UnitGraphics __instance)
        {
            if (__instance == null)
            {
                return;
            }
            if (__instance.Unit == null)
            {
                return;
            }
            if (!Utils.Helpers.IsPlayer(__instance.Unit))
            {
                return;
            }
            string text = (__instance.Pilot.Stats.Name + "startingLevel").ToUpper();
            if (__instance.Pilot.Stats.SaveExists(text))
            {
                Utils.Helpers.UpdatePlayerSkills(__instance.Pilot.Stats);

            }

            FM2_PersonalModPlugin.Log.LogInfo($"Successfully Reloaded Skills!");
        }

        

        [HarmonyPatch(typeof(AttackInstance), "DidHitRaw")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void Accuracy(AttackInstance __instance, AttackInstance.SingleHit hit, ref int attackChance, ref bool __result, PilotStats.PilotHandicap handicap)
        {

            BattleUnit attackerUnit = __instance.AttackerUnit;
            
            if (!(attackerUnit is PlayerUnit))
            {
                return;
            }
            attackChance = __instance.Settings.ProcessAccuracy(__instance, hit, true);
            attackChance = handicap.GetHit(attackChance);
            double multiplier = 1.4;
            FM2_PersonalModPlugin.Log.LogInfo($"Original Player Hit Chance: {attackChance}");
            if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
            {
                multiplier *= 2;
            }
            attackChance = (int)(attackChance * multiplier);
            FM2_PersonalModPlugin.Log.LogInfo($"Modified Player Hit Chance: {attackChance}, enemy Evade Chance: {hit.EvadeChance}");
            __result = Utility.CheckChance0To100(attackChance);
        }


        //Modify Skill Equip Limits
        //[HarmonyPatch(typeof(PilotSkills), MethodType.Constructor, new Type[] { typeof(PilotSkills) })]
        //[HarmonyPriority(0)]
        //[HarmonyPrefix]
        public static bool PilotSkills(PilotSkills __instance, PilotSkills skills)
        {
            if (skills.Equipped == null)
            {
                skills.Equipped = new Skill[SkillLimit];
            }
            if (skills.Earned == null)
            {
                skills.Earned = new List<Skill>();
            }
            if (skills.Equipped.Length < SkillLimit)
            {
                __instance.Equipped = new Skill[SkillLimit];
                for (int i = 0; i < skills.Equipped.Length; i++)
                {
                    __instance.Equipped[i] = skills.Equipped[i];
                }
                skills.Equipped = __instance.Equipped;
            }
            else
            {
                __instance.Equipped = new Skill[SkillLimit];
                for (int j = 0; j < SkillLimit; j++)
                {
                    __instance.Equipped[j] = skills.Equipped[j];
                }
            }
            __instance.Earned = new List<Skill>(skills.Earned);
            __instance.CheckDoubledSkills();
            bool[] dirty = Utils.Helpers.GetPrivateField<bool[]>(__instance, "dirty");
            int[] stats = Utils.Helpers.GetPrivateField<int[]>(__instance, "stats");
            dirty = new bool[Enum.GetNames(typeof(SkillType)).Length];
            for (int k = 0; k < dirty.Length; k++)
            {
                dirty[k] = true;
            }
            Utils.Helpers.SetPrivateField<bool[]>(__instance, "dirty", dirty);
            stats = new int[Enum.GetNames(typeof(SkillType)).Length];
            Utils.Helpers.SetPrivateField<int[]>(__instance, "stats", stats);
            return false;
        }

        [HarmonyPatch(typeof(EquippedSkillsPanel), "UpdateStats")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void UpdateSkills(EquippedSkillsPanel __instance)
        {
            UnitGraphics selectedUnitGraphics = Utils.Helpers.GetPrivateField<UnitGraphics>(__instance, "selectedUnitGraphics");
            PilotStats stats = selectedUnitGraphics.Pilot.Stats;
            PilotSkills skills = stats.Skills;
            foreach (Skill skill in skills.Equipped)
            {
                if (skill != null)
                {
                    FM2_PersonalModPlugin.Log.LogInfo($"Currently Equipped Skills: SkillName: {skill.SkillName}");
                }
            }
            //Utils.Helpers.DumpSprite(stats.Picture, stats.Picture.name);
            Utils.Helpers.UpdatePlayerSkills(stats);
        }

        [HarmonyPatch(typeof(BattleCalculationSettings), "FinalDamage")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void Damage(BattleCalculationSettings __instance, ref int __result, ValueTuple<float, Elements[]> attackPower, AttackInstance attack, Part chosenPart)
        {
            double multiplier = 1;
            if (attack.AttackerUnit.unitTeam != Unit.UnitTeam.PlayerUnit)
            {
                return;
            }
            FM2_PersonalModPlugin.Log.LogInfo($"Original Player Attack Damage: {__result}");

            multiplier *= Utils.Helpers.UpdateGameDifficultyAtk(__instance.Difficulty, attack);
            __result = (int)(__result * multiplier);
            FM2_PersonalModPlugin.Log.LogInfo($"Boosted Damage: {__result}, Damage Multiplier: {Utils.Helpers.UpdateGameDifficultyAtk(__instance.Difficulty, attack)}");
        }

        

        [HarmonyPatch(typeof(Machine), "RefreshMove")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void BoostMove(Machine __instance)
        {
            double multiplier = 1;
            if (__instance == null)
            {
                return;
            }
            if (__instance.Stats == null)
            {
                return;
            }
            if (__instance.Stats.Owner == null)
            {
                return;
            }
            //FM2_PersonalModPlugin.Log.LogInfo($"In Calculate Move Patch!");
            if (!Utils.Helpers.IsPlayer(__instance.Stats.Owner))
            {
                return;
            }
            if (Utils.Helpers.SpecialPilots.Contains(__instance.Stats.Owner.Pilot.Stats.Callsign))
            {
                multiplier *= 1.5;
            }
            if (Utils.Helpers.CommanderPilots.Contains(__instance.Stats.Owner.Pilot.Stats.Callsign))
            {
                multiplier *= 1.25;
            }
            multiplier *= 1.25;
            
            int move = (int)(__instance.Move * multiplier);
            Utils.Helpers.SetPrivateProperty<int>(__instance, "Move", move);
        }

        [HarmonyPatch(typeof(BattleUnit), "MaxMovementRange")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void MaxMovement(BattleUnit __instance, ref int __result)
        {
            if (__instance == null)
            {
                return;
            }
            if (!Utils.Helpers.IsPlayer(__instance))
            {
                return;
            }         
            __result = 999;
        }

        [HarmonyPatch(typeof(BodyStats), "get_PowerOutput")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void get_Output(BodyStats __instance,ref int __result)
        {
            double multiplier = 1;
            if (__instance == null)
            {
                return;
            }
            if (__instance.Owner == null)
            {
                return;
            }
            //FM2_PersonalModPlugin.Log.LogInfo($"In PowerOutput Patch!");
            Unit unit = __instance.Owner;
            if (!Utils.Helpers.IsPlayer(unit))
            {
                return;
            }
            if (Utils.Helpers.SpecialPilots.Contains(unit.Pilot.Stats.Callsign))
            {
                multiplier *= 2;
            }
            if (Utils.Helpers.CommanderPilots.Contains(unit.Pilot.Stats.Callsign))
            {
                multiplier *= 1.5;
            }
            multiplier *= 1.75;
            //Utils.Helpers.SetPrivateProperty<int>(__instance, "PowerOutput", 999);
            __result = (int)(__result * multiplier);
        }

        static readonly AccessTools.FieldRef<HonorSystem, GridPathfinder> pathfinderRef = AccessTools.FieldRefAccess<HonorSystem, GridPathfinder>("pathfinder");
        static readonly AccessTools.FieldRef<GridPathfinder, Unit> unitRef = AccessTools.FieldRefAccess<GridPathfinder, Unit>("unit");

        [HarmonyPatch(typeof(HonorSystem), "CheckHonorSkills", new Type[] { })]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static bool CheckInfluenced(HonorSystem __instance)
        {
            //Modified via a postfix patch to return all units within 3 Tile Range (modify it in helpers)
            List<BattleUnit> influencedUnits = Utils.Helpers.GetPrivateField<List<BattleUnit>>(__instance, "influencedUnits");
            List<BattleUnit> unitsInRange = (List<BattleUnit>)Utils.Helpers.Call(__instance, "GetUnitsInRange");
            List<BattleUnit> underInfluence = Utils.Helpers.GetBattleUnits(__instance.UnderInfluence);          
            List<BattleUnit> allunits = new List<BattleUnit>();
            allunits.AddRange(influencedUnits);
            allunits.AddRange(unitsInRange);
            allunits.AddRange(underInfluence);

            //This is the owner's actual honor range
            int ownRange = Utils.Helpers.GetCustomRange(__instance.Owner.Pilot, __instance.Owner.unitTeam);
            foreach (BattleUnit battleUnit in allunits)
            {
                int range = Utils.Helpers.GetCustomRange(battleUnit.Pilot, battleUnit.unitTeam);
                //Need to check if the owner is actually in range using own range
                
                if (!Utils.Helpers.IsWithinRange(__instance.Owner, battleUnit, ownRange))
                {
                    Utils.Helpers.Call(__instance, "StopInfluenceUnit", battleUnit);
                    
                }
                //Need to check the reverse, if the battleUnit is in range with the owner using its range
                //FM2_PersonalModPlugin.Log.LogInfo($"Is {__instance.Owner} within the range of {battleUnit}? {Utils.Helpers.IsWithinRange(battleUnit, __instance.Owner, range)}");
                if (!Utils.Helpers.IsWithinRange(battleUnit, __instance.Owner, range))
                {
                    if (battleUnit.Honor != null)
                    {
                        //FM2_PersonalModPlugin.Log.LogError($"Stopping Influence of Unit: {battleUnit} on Unit: {__instance.Owner}");
                        Utils.Helpers.Call(battleUnit.Honor, "StopInfluenceUnit", __instance.Owner);
                    }
                       
                }
            }

            foreach (BattleUnit battleUnit2 in allunits)
            {
                int range = Utils.Helpers.GetCustomRange(battleUnit2.Pilot, battleUnit2.unitTeam);
                List<BattleUnit> unit_influencedUnits = Utils.Helpers.GetPrivateField<List<BattleUnit>>(battleUnit2.Honor, "influencedUnits");
                if (Utils.Helpers.IsWithinRange(__instance.Owner, battleUnit2, ownRange))
                {
                    if (!influencedUnits.Contains(battleUnit2))
                    {
                        Utils.Helpers.Call(__instance, "InfluenceUnit", battleUnit2);
                    }
                }
                if (Utils.Helpers.IsWithinRange(battleUnit2, __instance.Owner, range))
                {
                    if (!unit_influencedUnits.Contains(__instance.Owner))
                    {
                        if (battleUnit2.Honor != null)
                        {
                            Utils.Helpers.Call(battleUnit2.Honor, "InfluenceUnit", __instance.Owner);
                        }
                    }
                        

                }           

            }

            //Logging Purposes
            if (!Utils.Helpers.IsPlayer(__instance.Owner))
            {
                return false;
                
            }
            FM2_PersonalModPlugin.Log.LogError($"Printing influenced units of Owner: {__instance.Owner}");
            List<BattleUnit> units = Utils.Helpers.GetPrivateField<List<BattleUnit>>(__instance, "influencedUnits");
            if (units == null)
            {
                return false;
            }
            foreach (BattleUnit unit in units)
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Influencing Unit: {unit}");
            }
            foreach (ValueTuple<HonorSkillBase, BattleUnit> valueTuple in __instance.UnderInfluence)
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Influenced By Unit: {valueTuple.Item2}, Honor Skill: {valueTuple.Item1.SkillName}");
            }
            return false;
            
        }

        [HarmonyPatch(typeof(HonorSystem), "GetUnitsInRange")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void HonorRange(HonorSystem __instance, ref List<BattleUnit> __result)
        {   
            //This should apply to every unit!
            List<BattleUnit> units = new List<BattleUnit>();
            GridPathfinder pathfinder = pathfinderRef(__instance);
            //Unit unit = unitRef(pathfinder);
            Unit unit = __instance.Owner;
            Tile tile = unit.MovementController.OccupiedTile ?? unit.MovementController.OccupiedTileBeforeRemoved;
            int range = Utils.Helpers.MaxHonorRange;
            List<Tile> inRange = Utils.Helpers.GetTilesInRange(Grid.Instance, tile, range);
            foreach (Tile item in inRange)
            {
                if (!item.Unit || item.Unit == __instance.Owner)
                {
                    continue;
                }
                BattleUnit battleUnit = item.Unit as BattleUnit;
                if (battleUnit != null || item.Unit.TryGetComponent<BattleUnit>(out battleUnit))
                {
                    units.Add(battleUnit);
                }
            }
            __result = units;
        }

        [HarmonyPatch(typeof(PartStats), nameof(PartStats.SetDifficultyHP))]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void SetHP(PartStats __instance, DifficultyLevelSettings difficulty)
        {
            if (__instance == null) 
            {
                return;
            }
            if (__instance.Owner == null) 
            {
                return;
            }
            if (!Utils.Helpers.IsPlayer(__instance.Owner))
            {
                return;
            }
            
            Unit unit = __instance.Owner;
            Pilot pilot = unit.Pilot;
            string Name = pilot.Stats.Callsign;
            int maxHP = Utils.Helpers.UpdateGameDifficultyHp(difficulty, __instance);
            FM2_PersonalModPlugin.Log.LogInfo($"Patching Unit of Pilot: {Name}, Pilot.SaveID: {pilot.SaveID}");
            pilot.Stats.ForceMaxAP(60);
            Utils.Helpers.SetPrivateProperty<bool>(pilot.Stats, "ImmuneToSurrender", true);
            Utils.Helpers.SetPrivateProperty<int>(__instance, "MaxHp", maxHP);
            __instance.CurrentHp = maxHP;
            //pilot.Stats.Skills.CheatEarnAll();
            //pilot.Stats.Skills.Equipped = Utils.Helpers.UpdatePlayerSkills(pilot.Stats);
            if (__instance.GetType() == typeof(BodyStats)) 
            {
                Utils.Helpers.UpdateUnitScale(unit);
            }
            FM2_PersonalModPlugin.Log.LogInfo("Successfully patched player unit HP!");
        }

        [HarmonyPatch(typeof(PilotStats), "get_Picture")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void LoadPicture(PilotStats __instance, ref Sprite __result)
        {
            //FM2_PersonalModPlugin.Log.LogInfo("In LoadPicture Patch!");
            __result = Utils.Helpers.GetReplacementSprite(Utils.Helpers.Images, __result);
        }

        [HarmonyPatch(typeof(AchievementsManager), "UnlockAchievement")]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static bool Achievement()
        {
            return false;
        }
    }
}
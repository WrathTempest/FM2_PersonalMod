using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Diagnostics;

namespace FM2_PersonalMod.Patches
{
    [HarmonyPatch]
    internal class MainPatches
    {
        private static readonly int SkillLimit = 20;

        [HarmonyPatch(typeof(UnitsScaleChanger), "SetUnitScale")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
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
                    d *= (float)1.2;
                }
                else if (Utils.Helpers.CommanderPilots.Contains(unit.Pilot.Stats.Callsign))
                {
                    d *= (float)1.12;
                }
                FM2_PersonalModPlugin.Log.LogInfo($"Player Unit Scale Modifier: {d}");
                FM2_PersonalModPlugin.Log.LogInfo($"Player Unit Local Scale: {localScale}");
            }
            unitsScaleDataBase.SaveScale(unit, localScale);
            unit.gameObject.transform.localScale *= d;

        }

        [HarmonyPatch(typeof(BattleSkillBase), nameof(BattleSkillBase.GetActivationChance))]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ActivationChance(BattleSkillBase __instance, ref float __result, AttackInstance attackInstance)
        {
            BattleUnit attackerUnit = attackInstance.AttackerUnit;
            BattleUnit defenderUnit = attackInstance.DefenderUnit;

            FM2_PersonalModPlugin.Log.LogInfo($"Original Skill Name: {__instance.SkillName}, Activation Chance: {__result}");

            if (attackerUnit is PlayerUnit)
            {
                __result *= (float)1.5;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    __result *= (float)2;
                }
            }
            if (defenderUnit is PlayerUnit)
            {
                __result *= (float)1.5;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    __result *= (float)2;
                }
            }

            FM2_PersonalModPlugin.Log.LogInfo($"Skill Trigger Chance Modified! Skill Name: {__instance.SkillName}, Activation Chance: {__result}");

        }

        [HarmonyPatch(typeof(BattleSkillBase), "GetChainProbability")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void ChainChance(BattleSkillBase __instance, ref float __result, AttackInstance attackInstance)
        {
            BattleUnit attackerUnit = attackInstance.AttackerUnit;
            BattleUnit defenderUnit = attackInstance.DefenderUnit;

            if (attackerUnit is PlayerUnit)
            {
                __result *= (float)1.5;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    __result *= (float)1.2;
                }
            }
            if (defenderUnit is PlayerUnit)
            {
                __result *= (float)1.5;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    __result *= (float)1.5;
                }
            }
            FM2_PersonalModPlugin.Log.LogInfo($"Skill Chain Chance Modified! Skill Name: {__instance.SkillName}, Chain Chance: {__result}");

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

        

        [HarmonyPatch(typeof(BattleCalculationSettings), "ProcessAccuracy")]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static void Accuracy(BattleCalculationSettings __instance, AttackInstance attack, AttackInstance.SingleHit hit)
        {
            double multiplier = 1;
            BattleUnit attackerUnit = attack.AttackerUnit;
            BattleUnit defenderUnit = attack.DefenderUnit;
            FM2_PersonalModPlugin.Log.LogInfo($"Original Player Hit Chance: {hit.AttackChance}");
            if (attackerUnit is PlayerUnit)
            {
                multiplier *= 1.3;
                if (Utils.Helpers.SpecialPilots.Contains(attackerUnit.Pilot.Stats.Callsign))
                {
                    multiplier *= 2;
                }
            }
            attack.MultiplyAllAttackChances((float)multiplier);
            hit.MultiplyAttackChance((float)multiplier);
            FM2_PersonalModPlugin.Log.LogInfo($"Modified Player Hit Chance: {hit.AttackChance}, enemy Evade Chance: {hit.EvadeChance}");
        }


        //Modify Skill Equip Limits
        [HarmonyPatch(typeof(PilotSkills), MethodType.Constructor, new Type[] { typeof(PilotSkills) })]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
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
            foreach (Skill skill in selectedUnitGraphics.Pilot.Stats.Skills.Equipped)
            {
                if (skill != null)
                {
                    FM2_PersonalModPlugin.Log.LogInfo($"Currently Equipped Skills: SkillName: {skill.SkillName}");
                }
            }
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

        

        [HarmonyPatch(typeof(Machine), "CalculateMove")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void BoostMove(Machine __instance, ref int __result)
        {
            double multiplier = 1;
            if (__instance == null)
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
            multiplier *= 1.5;
            //FM2_PersonalModPlugin.Log.LogInfo($"Boosted Movement by: {multiplier}");
            __result = (int)(__result * multiplier);
        }

        [HarmonyPatch(typeof(BattleUnit), "MaxMovementRange")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void BoostMove(BattleUnit __instance, ref int __result)
        {
            double multiplier = 1;
            if (__instance == null)
            {
                return;
            }
           // FM2_PersonalModPlugin.Log.LogInfo($"In Calculate Move Patch!");
            if (!Utils.Helpers.IsPlayer(__instance))
            {
                return;
            }
            if (Utils.Helpers.SpecialPilots.Contains(__instance.Pilot.Stats.Callsign))
            {
                multiplier *= 1.5;
            }
            multiplier *= 1.5;
            //FM2_PersonalModPlugin.Log.LogInfo($"(BattleUnit) Boosted Movement by: {multiplier}");
            __result = (int)(__result * multiplier);
        }

        [HarmonyPatch(typeof(BodyStats), "get_PowerOutput")]
        [HarmonyPriority(0)]
        [HarmonyPostfix]
        public static void get_Output(BodyStats __instance,ref int __result)
        {
            if (__instance == null)
            {
                return;
            }
            if (__instance.Owner == null)
            {
                return;
            }
            //FM2_PersonalModPlugin.Log.LogInfo($"In PowerOutput Patch!");
            if (!Utils.Helpers.IsPlayer(__instance.Owner))
            {
                return;
            }
            Utils.Helpers.SetPrivateProperty<int>(__instance, "PowerOutput", 999);
            __result = 999;
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

        [HarmonyPatch(typeof(AchievementsManager), "UnlockAchievement")]
        [HarmonyPriority(0)]
        [HarmonyPrefix]
        public static bool Achievement()
        {
            return false;
        }
    }
}
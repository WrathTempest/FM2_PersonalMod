using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FM2_PersonalMod.Utils
{
    /// <summary>
    /// Static utilities class for common functions and properties to be used within your mod code
    /// </summary>
    internal static class Helpers
    {
        public static List<string> SpecialSkills = new List<string>()
        {
            "Dash Lv4",
            "Z.O.C.",
            "High Speed Lv4",
            "Hunting Lv4",
            "Switch Lv4",
            "Terror Shot Lv4",
            "Sniper Lv4",
            "Immortal Lv4",
            "Invalid Honor",
            "Skill Control"
        };

        public static List<string> CommanderSkills = new List<string>()
        {
            "Dash Lv4",
            "Z.O.C.",
            "Terror Shot Lv4",
            "Immortal Lv4",
            "Invalid Honor",
            "Skill Control"
        };

        public static List<string> SpecialPilots = new List<string>()
        {
            "Noir",
            "Ash"
        };

        public static List<string> CommanderPilots = new List<string>()
        {
            "Lisa",
            "Amia"
        };

        private static readonly Dictionary<DifficultyManager.DifficultyLevels, double> DifficultyHPBoost = new Dictionary<DifficultyManager.DifficultyLevels, double>() 
        {
            {DifficultyManager.DifficultyLevels.VeryEasy, 1},
            {DifficultyManager.DifficultyLevels.Easy, 1},
            {DifficultyManager.DifficultyLevels.Normal, 1.5},
            {DifficultyManager.DifficultyLevels.Hard, 2},
            {DifficultyManager.DifficultyLevels.Extreme, 3},
            {DifficultyManager.DifficultyLevels.Impossible, 4}
        };

        private static readonly Dictionary<DifficultyManager.DifficultyLevels, double> DifficultyAtkBoost = new Dictionary<DifficultyManager.DifficultyLevels, double>()
        {
            {DifficultyManager.DifficultyLevels.VeryEasy, 1},
            {DifficultyManager.DifficultyLevels.Easy, 1},
            {DifficultyManager.DifficultyLevels.Normal, 1.2},
            {DifficultyManager.DifficultyLevels.Hard, 1.4},
            {DifficultyManager.DifficultyLevels.Extreme, 1.7},
            {DifficultyManager.DifficultyLevels.Impossible, 2}
        };
        public static bool IsPlayer(Unit unit)
        {     
            return unit.unitTeam == Unit.UnitTeam.PlayerUnit;
        }

        public static bool IsPlayer(BattleUnit unit)
        {
            return unit.unitTeam == Unit.UnitTeam.PlayerUnit;
        }

        public static void ResetSkills(PilotStats stats)
        {
            Skill[] skills = stats.Skills.Equipped;
            stats.Skills.Equipped = new Skill[skills.Length];
            stats.Skills.CheatEarnAll();           
            return;
        }

        public static void UpdateUnitScale (Unit unit)
        {
            float scaleMultiplier = 1;
            if (SpecialPilots.Contains(unit.Pilot.Stats.Callsign))
            {
                scaleMultiplier = (float)1.35;
            }
            else if (CommanderPilots.Contains(unit.Pilot.Stats.Callsign))
            {
                scaleMultiplier = (float)1.2;
            }
            FM2_PersonalModPlugin.Log.LogInfo($"Vanilla Scale:{unit.transform.localScale}");
            unit.transform.localScale *= scaleMultiplier;    
        }

        private static Skill[] AddSkills(Skill[] skills, PilotStats stats, List<string> SkillSet = null)
        {
            if (SkillSet == null)
            {
                SkillSet = new List<string>();
            }
            List<Skill> currentskills = skills.ToList();
            Skill[] HonorSkills = SaveUtility.GetAllInstances<HonorSkillBase>();
            Skill[] BattleSkills = SaveUtility.GetAllInstances<BattleSkillBase>();
            List<Skill> AllSkills = new List<Skill>();
            AllSkills.AddRange(HonorSkills);
            AllSkills.AddRange(BattleSkills);
            foreach (Skill skill in AllSkills)
            {
                bool notEquipped = !currentskills.Contains(skill);
                bool notEarned = !stats.Skills.Earned.Contains(skill);
                bool additionalSkill = SkillSet.Contains(skill.SkillName);
                if (notEquipped && additionalSkill && notEarned)
                {
                    FM2_PersonalModPlugin.Log.LogInfo($"Equipping Skill: {skill.SkillName}");
                    currentskills.Add(skill);
                }
                else if (notEquipped && additionalSkill && !notEarned)
                {
                    FM2_PersonalModPlugin.Log.LogInfo($"Equipping Skill: {skill.SkillName}");
                    currentskills.Add(skill);
                    stats.Skills.Earned.Remove(skill);
                }
                else if (notEquipped && !additionalSkill && notEarned)
                {
                    FM2_PersonalModPlugin.Log.LogInfo($"Skill found that is not equipped and not learned, Adding skill: {skill.SkillName}");
                    Call(stats.Skills, "EarnSkill", skill);
                    
                }
            }               
            stats.Skills.Earned = (from x in stats.Skills.Earned orderby x.SkillName select x).ToList<Skill>();
            return currentskills.ToArray();
        }

        public static void UpdatePlayerSkills(PilotStats stats)
        {
            Skill[] skills = stats.Skills.Equipped;
            if (SpecialPilots.Contains(stats.Callsign))
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Special Pilot: {stats.Callsign}...");
                skills = AddSkills(skills, stats, SpecialSkills);
                FM2_PersonalModPlugin.Log.LogInfo($"Successfully Added Skills!");
            }
            else if (CommanderPilots.Contains(stats.Callsign))
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Commander Pilot: {stats.Callsign}...");
                skills = AddSkills(skills, stats, CommanderSkills);
                FM2_PersonalModPlugin.Log.LogInfo($"Successfully Added Skills!");
            }
            else
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Pilot: {stats.Callsign}...");
                skills = AddSkills(skills, stats);
                FM2_PersonalModPlugin.Log.LogInfo($"Successfully Added Skills!");
            }
            stats.Skills.Equipped = skills;
            stats.Skills.CheckDoubledSkills();
        }

        public static double UpdateGameDifficultyAtk(DifficultyLevelSettings difficulty, AttackInstance __instance)
        {
            double multiplier = 1;
            DifficultyManager.DifficultyLevels Difficulty = difficulty.difficulty;
            multiplier *= DifficultyAtkBoost[Difficulty];
            if (SpecialPilots.Contains(__instance.AttackerUnit.Pilot.Stats.Callsign))
            {
                multiplier *= 1.5;
            }
            else if (CommanderPilots.Contains(__instance.AttackerUnit.Pilot.Stats.Callsign))
            {
                multiplier *= 1.2;
            }
            return multiplier;

        }
        public static int UpdateGameDifficultyHp(DifficultyLevelSettings difficulty, PartStats __instance)
        {
            double multiplier = 1;
            DifficultyManager.DifficultyLevels Difficulty = difficulty.difficulty;
            multiplier *= DifficultyHPBoost[Difficulty];
            if (SpecialPilots.Contains(__instance.Owner.Pilot.Stats.Callsign))
            {
                multiplier *= 2;
            }
            else if (CommanderPilots.Contains(__instance.Owner.Pilot.Stats.Callsign))
            {
                multiplier *= 1.4;
            }
            double HP = __instance.OriginalHP * multiplier;
            FM2_PersonalModPlugin.Log.LogInfo($"HP Multiplier: {multiplier}, Original HP: {__instance.OriginalHP}, Modified HP: {HP}");
            return (int)HP;
        }
        public static T GetPrivateField<T>(object instance, string fieldName)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            // Use AccessTools to get the field
            return AccessTools.FieldRefAccess<T>(instance.GetType(), fieldName)(instance);
        }
        public static void SetPrivateField<T>(object instance, string fieldName, T newValue)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException(nameof(fieldName));

            Type type = instance.GetType();
            FieldInfo field = null;

            while (type != null)
            {
                field = type.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (field != null)
                    break;

                type = type.BaseType;
            }

            if (field == null)
                throw new MissingFieldException(instance.GetType().FullName, fieldName);

            field.SetValue(instance, newValue);
        }

        public static T GetPrivateProperty<T>(object instance, string propertyName)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            // Try property first
            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                return (T)prop.GetValue(instance, null);
            }

            // Fallback: try getter method directly (get_PropertyName)
            var getter = type.GetMethod(
                "get_" + propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (getter != null)
            {
                return (T)getter.Invoke(instance, null);
            }

            throw new MissingMemberException(type.FullName, propertyName);
        }

        public static void SetPrivateProperty<T>(object instance, string propertyName, T value)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var type = instance.GetType();

            var prop = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (prop != null)
            {
                prop.SetValue(instance, value, null);
                return;
            }

            var setter = type.GetMethod(
                "set_" + propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (setter != null)
            {
                setter.Invoke(instance, new object[] { value });
                return;
            }

            throw new MissingMemberException(type.FullName, propertyName);
        }

        public static string GetRealCaller(int skipFrames = 1)
        {
            var stackTrace = new StackTrace(skipFrames, true);
            foreach (var frame in stackTrace.GetFrames())
            {
                MethodBase method = frame.GetMethod();
                if (method == null) continue;

                // Skip Harmony-generated dynamic methods
                if (method.Name.Contains("DMD<")) continue;

                // Skip helper class itself
                if (method.DeclaringType == typeof(Helpers)) continue;

                return $"{method.DeclaringType.FullName}.{method.Name}";
            }

            return "UnknownCaller";
        }

        /// <summary>
        /// Logs a traced call for debugging.
        /// </summary>
        public static void LogCaller(string message = "", int skipFrames = 1)
        {
            string caller = GetRealCaller(skipFrames + 1); // +1 for hero.battleDataBehaviour.battleData method
            FM2_PersonalModPlugin.Log.LogInfo($"{message} Called by: {caller}");
        }

        public static object Call(
        object instance,
        string methodName,
        params object[] args)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            Type type = instance.GetType();

            MethodInfo method = AccessTools.Method(type, methodName);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            return method.Invoke(instance, args);
        }

        public static object CallStatic(
            Type type,
            string methodName,
            params object[] args)
        {
            MethodInfo method = AccessTools.Method(type, methodName);

            if (method == null)
                throw new MissingMethodException(type.FullName, methodName);

            return method.Invoke(null, args);
        }
    }
}

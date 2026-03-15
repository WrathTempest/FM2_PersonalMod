using HarmonyLib;
using Pathfinding;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        static readonly AccessTools.FieldRef<Grid, BattleGridGenerator> gridGeneratorRef = AccessTools.FieldRefAccess<Grid, BattleGridGenerator>("gridGenerator");
        public static Dictionary<string, Sprite> Images = new Dictionary<string, Sprite>();
        public static readonly int MaxHonorRange = 3;
        public static readonly int DefaultChainCount = 4;
        public static List<string> SpecialSkills = new List<string>()
        {
            "Dash Lv4",
            "Z.O.C.",
            "High Speed Lv4",
            "Hunting Lv4",
            "Switch Lv4",
            "Feint Lv4",
            "Sniper Lv4",
            "Immortal Lv4",
            "Guide Lv4",
            "Invalid Honor",
            "Invalid APB",
            "Skill Control",
            "Skill Up Lv2",
            "Morale",
            "Surrender Call"
        };

        public static List<string> CommanderSkills = new List<string>()
        {
            "Dash Lv4",
            "Z.O.C.",
            "Immortal Lv4",
            "Guide Lv4",
            "Invalid Honor",
            "Invalid APB",
            "Skill Up Lv2",
            "Skill Down Lv2",
            "Morale",
            "Surrender Call"
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

        public static int GetChainLimit(BattleUnit unit)
        {
            int limit = DefaultChainCount;
            string name = unit.Pilot.Stats.Callsign;
            if (SpecialPilots.Contains(name))
            {
                limit += 2;
            }
            if (CommanderPilots.Contains(name))
            {
                limit += 1;
            }
            return limit;
        }
        public static bool IsWithinRange(BattleUnit owner, BattleUnit target, int range)
        {
            Tile ownerTile = owner.MovementController.OccupiedTile ?? owner.MovementController.OccupiedTileBeforeRemoved;
            Tile targetTile = target.MovementController.OccupiedTile ?? target.MovementController.OccupiedTileBeforeRemoved;

            if (ownerTile == null || targetTile == null) return false;

            int dx = Math.Abs(ownerTile.X - targetTile.X);
            int dz = Math.Abs(ownerTile.Z - targetTile.Z);

            if (range == 1)
            {
                return (dx + dz) <= range;
            }
            return (dx + dz) <= range;
        }

        public static int GetCustomRange(Pilot pilot, Unit.UnitTeam team)
        {
            String callsign = pilot.Stats.Callsign;
            if (team != Unit.UnitTeam.PlayerUnit)
            {
                return 1;
            }
            if (SpecialPilots.Contains(callsign))
            {
                return 3;
            }
            if (CommanderPilots.Contains(callsign))
            {
                return 2;
            }          
            return 1;
        }

        public static List<Tile> GetTilesInRange(Grid grid, Tile tile, int range)
        {
            List<Tile> list = new List<Tile>();
            BattleGridGenerator gridGenerator = gridGeneratorRef(grid);
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dz = -range; dz <= range; dz++)
                {
                    // Manhattan distance check
                    if (Math.Abs(dx) + Math.Abs(dz) > range)
                        continue;

                    int x = tile.X + dx;
                    int z = tile.Z + dz;

                    // Skip center tile
                    if (dx == 0 && dz == 0)
                        continue;

                    // Bounds check
                    if (x < 0 || z < 0 || x >= gridGenerator.MapWidth || z >= gridGenerator.MapHeight)
                        continue;

                    list.Add(grid.gridArray[x, z]);
                }
            }

            return list;
        }
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
                scaleMultiplier = (float)1.45;
            }
            else if (CommanderPilots.Contains(unit.Pilot.Stats.Callsign))
            {
                scaleMultiplier = (float)1.22;
            }
            FM2_PersonalModPlugin.Log.LogInfo($"Vanilla Scale:{unit.transform.localScale}");
            unit.transform.localScale *= scaleMultiplier;    
        }

        public static List<BattleUnit> GetBattleUnits(List<ValueTuple<HonorSkillBase, BattleUnit>> tuple)
        {
            List<BattleUnit> units = new List<BattleUnit>();
            if (tuple == null)
            {
                return units;
            }
            if (tuple.Count <=0)
            {
                return units;
            }
            foreach (ValueTuple<HonorSkillBase, BattleUnit> vt in tuple)
            {
                units.Add(vt.Item2);
            }
            return units;
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
            Skill[] equipped = new Skill[4];
            int size = Math.Min(skills.Length, equipped.Length);
            for (int i = 0; i < size; i++) 
            {
                if (skills[i] != null)
                {
                    equipped[i] = skills[i];
                }
            }
            if (SpecialPilots.Contains(stats.Callsign))
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Special Pilot: {stats.Callsign}...");
                skills = AddSkills(equipped, stats, SpecialSkills);
                FM2_PersonalModPlugin.Log.LogInfo($"Successfully Added Skills!");
            }
            else if (CommanderPilots.Contains(stats.Callsign))
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Commander Pilot: {stats.Callsign}...");
                skills = AddSkills(equipped, stats, CommanderSkills);
                FM2_PersonalModPlugin.Log.LogInfo($"Successfully Added Skills!");
            }
            else
            {
                FM2_PersonalModPlugin.Log.LogInfo($"Attempting to Modify Skills of Pilot: {stats.Callsign}...");
                skills = AddSkills(equipped, stats);
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

        public static void DumpSprite(Sprite sprite, string fileName)
        {
            if (sprite == null)
                return;

            Texture2D source = sprite.texture;
            Rect rect = sprite.rect;

            Texture2D readableTex;

            // If texture is readable we can use it directly
            if (source.isReadable)
            {
                readableTex = source;
            }
            else
            {
                RenderTexture rt = RenderTexture.GetTemporary(
                    source.width,
                    source.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB
                );

                Graphics.Blit(source, rt);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = rt;

                readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readableTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readableTex.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }

            // Crop sprite from atlas
            Texture2D tex = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGBA32, false, false);

            Color[] pixels = readableTex.GetPixels(
                (int)rect.x,
                (int)rect.y,
                (int)rect.width,
                (int)rect.height
            );

            tex.SetPixels(pixels);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();

            string pluginDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string dumpDir = System.IO.Path.Combine(pluginDir, "Image", "Dump");

            Directory.CreateDirectory(dumpDir);

            string path = System.IO.Path.Combine(dumpDir, fileName + ".png");

            File.WriteAllBytes(path, png);
        }

        public static Dictionary<string, Sprite> LoadReplacementSprites()
        {
            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

            string pluginDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string replaceDir = System.IO.Path.Combine(pluginDir, "Image", "Replace");

            if (!Directory.Exists(replaceDir))
                return sprites;

            string[] files = Directory.GetFiles(replaceDir, "*.png");

            foreach (string file in files)
            {
                byte[] data = File.ReadAllBytes(file);

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);

                Sprite sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                string key = System.IO.Path.GetFileNameWithoutExtension(file);

                sprites[key] = sprite;
            }

            return sprites;
        }

        public static Sprite GetReplacementSprite(Dictionary<string, Sprite> dict, Sprite original)
        {
            if (original == null || dict == null)
                return original;

            string name = original.name;

            int first = name.IndexOf('_');
            int second = first >= 0 ? name.IndexOf('_', first + 1) : -1;

            string key = second > 0 ? name.Substring(0, second) : name;

            if (dict.TryGetValue(key, out Sprite replacement))
            {
                return replacement;
            }

            // fallback to original sprite
            return original;
        }
    }
}

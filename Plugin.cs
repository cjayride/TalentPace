using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Cjayride.TalentPace
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("M2Valheim.TalentTree")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "cjayride.talentpace";
        public const string PluginName = "Talent Pace";
        public const string PluginVersion = "1.0.1";

        internal static Plugin Instance;
        internal static ConfigEntry<int> CapNone;
        internal static ConfigEntry<int> CapEikthyr;
        internal static ConfigEntry<int> CapElder;
        internal static ConfigEntry<int> CapBonemass;
        internal static ConfigEntry<int> CapModer;
        internal static ConfigEntry<int> CapYagluth;
        internal static ConfigEntry<bool> QueenRemovesCap;

        private void Awake()
        {
            Instance = this;
            CapNone = BindCap("Before any boss", 9);
            CapEikthyr = BindCap("After Eikthyr", 15);
            CapElder = BindCap("After the Elder", 21);
            CapBonemass = BindCap("After Bonemass", 36);
            CapModer = BindCap("After Moder", 54);
            CapYagluth = BindCap("After Yagluth", 75);
            QueenRemovesCap = Config.Bind(
                "Caps",
                "Queen or Fader removes the cap",
                true,
                "Personal defeat of the Queen or Fader removes the point cap. The saved talents were never deleted.");

            if (AccessTools.TypeByName("Core.TalentPointBudgetService") == null
                || AccessTools.TypeByName("Core.TalentRebuildService") == null)
            {
                Logger.LogError("Talent Tree types were not found. Talent Pace is off.");
                return;
            }

            Harmony harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(BudgetPatch));
            harmony.PatchAll(typeof(RebuildPatch));
            harmony.PatchAll(typeof(BossKeyPatch));
            CapHint.Apply(harmony);

            new Terminal.ConsoleCommand(
                "talentpace",
                "Print the Talent Pace cap for your character.",
                args =>
                {
                    Player player = Player.m_localPlayer;
                    if (player == null)
                    {
                        args.Context.AddString("Talent Pace: no local player.");
                        return;
                    }

                    Pace.Summary summary = Pace.Describe(player);
                    args.Context.AddString(summary.Line);
                });

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded.");
        }

        private ConfigEntry<int> BindCap(string key, int value)
        {
            return Config.Bind(
                "Caps",
                key,
                value,
                new ConfigDescription(
                    "Talent points that apply once this player has that boss key. Outer nodes past the cap stay saved and turn back on after the next boss.",
                    new AcceptableValueRange<int>(0, 500)));
        }

        internal static void Log(string message)
        {
            Instance.Logger.LogInfo(message);
        }
    }

    [HarmonyPatch]
    internal static class BudgetPatch
    {
        private static MethodBase TargetMethod()
        {
            Type budget = AccessTools.TypeByName("Core.TalentPointBudgetService");
            return AccessTools.DeclaredMethod(budget, "GetTotalPoints", new[] { typeof(Player), AccessTools.TypeByName("Core.TalentRuntime") });
        }

        private static void Postfix([HarmonyArgument(0)] Player player, ref int __result)
        {
            if (player == null)
            {
                return;
            }

            Pace.RememberNatural(player, __result);
            int cap = Pace.CapFor(player);
            if (cap < int.MaxValue && __result > cap)
            {
                __result = cap;
            }
        }
    }

    [HarmonyPatch]
    internal static class RebuildPatch
    {
        private static MethodBase TargetMethod()
        {
            Type rebuild = AccessTools.TypeByName("Core.TalentRebuildService");
            return AccessTools.DeclaredMethod(rebuild, "Rebuild", new[] { AccessTools.TypeByName("Core.TalentRuntime") });
        }

        private static void Postfix([HarmonyArgument(0)] object runtime)
        {
            Pace.TrimCache(runtime);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddUniqueKey))]
    internal static class BossKeyPatch
    {
        private static void Postfix(Player __instance, string name)
        {
            if (__instance == null || string.IsNullOrEmpty(name) || name.IndexOf("defeated_", StringComparison.Ordinal) < 0)
            {
                return;
            }

            Pace.MarkDirty(__instance);
        }
    }

    internal static class Pace
    {
        private const int Minor = 0;
        private const int Keystone = 1;
        private const int Mid = 2;

        private static readonly string[] BossOrder =
        {
            "defeated_eikthyr",
            "defeated_gdking",
            "defeated_bonemass",
            "defeated_dragon",
            "defeated_goblinking"
        };

        private static MethodInfo _tryGet;
        private static MethodInfo _buildCache;
        private static MethodInfo _sync;
        private static MethodInfo _getRuntime;
        private static FieldInfo _runtimes;
        private static PropertyInfo _isDirty;
        private static bool _reflected;
        private static bool _reflectFailed;

        internal struct Summary
        {
            public string Line;
        }

        internal static int CapFor(Player player)
        {
            if (player == null)
            {
                return Plugin.CapNone.Value;
            }

            if (Plugin.QueenRemovesCap.Value
                && (player.HaveUniqueKey("defeated_queen") || player.HaveUniqueKey("defeated_fader")))
            {
                return int.MaxValue;
            }

            int cap = Plugin.CapNone.Value;
            if (player.HaveUniqueKey(BossOrder[0])) cap = Plugin.CapEikthyr.Value;
            if (player.HaveUniqueKey(BossOrder[1])) cap = Plugin.CapElder.Value;
            if (player.HaveUniqueKey(BossOrder[2])) cap = Plugin.CapBonemass.Value;
            if (player.HaveUniqueKey(BossOrder[3])) cap = Plugin.CapModer.Value;
            if (player.HaveUniqueKey(BossOrder[4])) cap = Plugin.CapYagluth.Value;
            return cap;
        }

        internal static int NaturalFor(Player player)
        {
            if (player == null)
            {
                return 0;
            }

            long id = player.GetPlayerID();
            return _natural.TryGetValue(id, out int points) ? points : 0;
        }

        internal static void RememberNatural(Player player, int points)
        {
            if (player == null)
            {
                return;
            }

            _natural[player.GetPlayerID()] = points;
        }

        internal static string Hint(Player player)
        {
            int cap = CapFor(player);
            if (cap == int.MaxValue)
            {
                return null;
            }

            int stage = 0;
            if (player != null && player.HaveUniqueKey(BossOrder[0])) stage = 1;
            if (player != null && player.HaveUniqueKey(BossOrder[1])) stage = 2;
            if (player != null && player.HaveUniqueKey(BossOrder[2])) stage = 3;
            if (player != null && player.HaveUniqueKey(BossOrder[3])) stage = 4;
            if (player != null && player.HaveUniqueKey(BossOrder[4])) stage = 5;

            int natural = NaturalFor(player);
            bool reached = natural >= cap;
            string lead = reached ? "Talent cap reached: " : "Talent cap: ";
            if (stage >= 5)
            {
                if (!Plugin.QueenRemovesCap.Value)
                {
                    return lead + cap + " points apply.";
                }

                return lead + cap + " points. Kill the Queen or Fader to remove the cap.";
            }

            int nextCap = stage == 0 ? Plugin.CapEikthyr.Value
                : stage == 1 ? Plugin.CapElder.Value
                : stage == 2 ? Plugin.CapBonemass.Value
                : stage == 3 ? Plugin.CapModer.Value
                : Plugin.CapYagluth.Value;
            return lead + cap + " points. Kill " + NextBoss[stage] + " to raise it to " + nextCap + ".";
        }

        static readonly string[] NextBoss =
        {
            "Eikthyr",
            "the Elder",
            "Bonemass",
            "Moder",
            "Yagluth"
        };

        static readonly System.Collections.Generic.Dictionary<long, int> _natural = new System.Collections.Generic.Dictionary<long, int>();

        internal static Summary Describe(Player player)
        {
            string hint = Hint(player);
            return new Summary { Line = hint ?? "Talent Pace: no cap." };
        }

        internal static void MarkDirty(Player player)
        {
            if (!EnsureReflection())
            {
                return;
            }

            IDictionary runtimes = _runtimes.GetValue(null) as IDictionary;
            if (runtimes == null)
            {
                return;
            }

            long id = player.GetPlayerID();
            if (!runtimes.Contains(id))
            {
                return;
            }

            object runtime = runtimes[id];
            _isDirty.SetValue(runtime, true, null);
            _getRuntime.Invoke(null, new object[] { player });
        }

        internal static void TrimCache(object runtime)
        {
            if (runtime == null || !EnsureReflection())
            {
                return;
            }

            Player player = runtime.GetType().GetProperty("Player").GetValue(runtime, null) as Player;
            if (player == null)
            {
                return;
            }

            int cap = CapFor(player);
            if (cap == int.MaxValue)
            {
                return;
            }

            object state = runtime.GetType().GetProperty("State").GetValue(runtime, null);
            IDictionary ranks = state.GetType().GetProperty("Ranks").GetValue(state, null) as IDictionary;
            if (ranks == null || ranks.Count == 0)
            {
                return;
            }

            var nodes = new System.Collections.Generic.List<Node>(ranks.Count);
            foreach (DictionaryEntry entry in ranks)
            {
                string id = entry.Key as string;
                int rank = entry.Value is int value ? value : 0;
                if (string.IsNullOrEmpty(id) || rank <= 0)
                {
                    continue;
                }

                object[] args = { id, null };
                if (!(bool)_tryGet.Invoke(null, args) || args[1] == null)
                {
                    continue;
                }

                object def = args[1];
                Type defType = def.GetType();
                int ring = (int)defType.GetProperty("Ring").GetValue(def, null);
                int tier = (int)defType.GetProperty("Tier").GetValue(def, null);
                nodes.Add(new Node(id, ring, Cost(tier), rank));
            }

            nodes.Sort(delegate(Node a, Node b)
            {
                int ring = a.Ring.CompareTo(b.Ring);
                return ring != 0 ? ring : string.CompareOrdinal(a.Id, b.Id);
            });

            var kept = new System.Collections.Generic.Dictionary<string, int>(nodes.Count);
            int remaining = cap;
            int keptPoints = 0;
            bool trimmed = false;
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                int affordable = node.Cost <= 0 ? node.Rank : remaining / node.Cost;
                int apply = Math.Min(node.Rank, Math.Max(0, affordable));
                kept[node.Id] = apply;
                remaining -= apply * node.Cost;
                keptPoints += apply * node.Cost;
                if (apply != node.Rank)
                {
                    trimmed = true;
                }
            }

            if (!trimmed)
            {
                return;
            }

            var keys = new System.Collections.Generic.List<string>(ranks.Count);
            foreach (DictionaryEntry entry in ranks)
            {
                if (entry.Key is string id)
                {
                    keys.Add(id);
                }
            }

            Func<string, int> lookup = id => kept.TryGetValue(id, out int rank) ? rank : 0;
            object cache = _buildCache.Invoke(null, new object[] { keys, lookup });
            runtime.GetType().GetProperty("Cache").SetValue(runtime, cache, null);
            _sync.Invoke(null, new object[] { runtime });
            Plugin.Log(player.GetPlayerName() + " talent cap " + cap + ", " + keptPoints + " points active.");
        }

        private static int Cost(int tier)
        {
            if (tier == Minor) return 1;
            if (tier == Mid) return 2;
            if (tier == Keystone) return 3;
            return 3;
        }

        private static bool EnsureReflection()
        {
            if (_reflected)
            {
                return !_reflectFailed;
            }

            _reflected = true;
            Type registry = AccessTools.TypeByName("Core.TalentRegistry");
            Type definition = AccessTools.TypeByName("Core.TalentDefinition");
            Type rebuild = AccessTools.TypeByName("Core.TalentRebuildService");
            Type runtime = AccessTools.TypeByName("Core.TalentRuntime");
            Type service = AccessTools.TypeByName("Core.TalentRuntimeService");
            Type passives = AccessTools.TypeByName("Core.TalentPassivesService");
            _tryGet = AccessTools.DeclaredMethod(registry, "TryGet", new[] { typeof(string), definition.MakeByRefType() });
            _buildCache = AccessTools.DeclaredMethod(rebuild, "BuildCache");
            _sync = AccessTools.DeclaredMethod(passives, "SyncToPlayer", new[] { runtime });
            _getRuntime = AccessTools.DeclaredMethod(service, "Get", new[] { typeof(Player) });
            _runtimes = AccessTools.DeclaredField(service, "_runtimes");
            _isDirty = AccessTools.DeclaredProperty(runtime, "IsDirty");
            _reflectFailed = _tryGet == null || _buildCache == null || _sync == null || _getRuntime == null || _runtimes == null || _isDirty == null;
            if (_reflectFailed)
            {
                Plugin.Log("Talent Pace could not bind Talent Tree. Caps are off.");
            }

            return !_reflectFailed;
        }

        private struct Node
        {
            public Node(string id, int ring, int cost, int rank)
            {
                Id = id;
                Ring = ring;
                Cost = cost;
                Rank = rank;
            }

            public string Id;
            public int Ring;
            public int Cost;
            public int Rank;
        }
    }
}

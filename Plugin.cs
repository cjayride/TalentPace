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
        public const string PluginVersion = "1.2.5";

        internal static Plugin Instance;
        internal static ConfigEntry<int> CapNone;
        internal static ConfigEntry<int> CapEikthyr;
        internal static ConfigEntry<int> CapElder;
        internal static ConfigEntry<int> CapBonemass;
        internal static ConfigEntry<int> CapModer;
        internal static ConfigEntry<int> CapYagluth;
        internal static ConfigEntry<int> CapQueen;
        internal static ConfigEntry<int> CapFader;
        internal static ConfigEntry<bool> QueenRemovesCap;
        internal static ConfigEntry<bool> FaderRemovesCap;
        internal static ConfigEntry<bool> EnableEffectScaling;
        internal static ConfigEntry<float> MovementEffectScale;
        internal static ConfigEntry<float> DamageEffectScale;
        internal static ConfigEntry<float> ParryEffectScale;
        internal static ConfigEntry<bool> EnableTalentPointCap;
        internal static ConfigEntry<bool> EnableEvaSurtrRewards;
        internal static ConfigEntry<int> EvaCapPerBoss;
        internal static ConfigEntry<int> EvaJotunnPointGrant;
        internal static ConfigEntry<int> EvaCoinsMist;
        internal static ConfigEntry<int> EvaCoinsAsh;
        internal static ConfigEntry<int> EvaCoinsDeep;
        internal static ConfigEntry<bool> EnableAutoTrim;

        private void Awake()
        {
            Instance = this;
            CapNone = BindCap("Before any boss", 20);
            CapEikthyr = BindCap("After Eikthyr", 32);
            CapElder = BindCap("After the Elder", 48);
            CapBonemass = BindCap("After Bonemass", 58);
            CapModer = BindCap("After Moder", 72);
            CapYagluth = BindCap("After Yagluth", 88);
            CapQueen = BindCap("After the Queen", 105);
            CapFader = BindCap("After the Fader", 120);
            QueenRemovesCap = Config.Bind(
                "Caps",
                "Queen removes the cap",
                false,
                "When true, a personal Queen defeat removes the point cap entirely. When false, use the After the Queen cap.");
            FaderRemovesCap = Config.Bind(
                "Caps",
                "Fader removes the cap",
                true,
                "The last boss. When true, a personal Fader defeat removes the point cap entirely. When false, use the After the Fader cap.");
            EnableTalentPointCap = Config.Bind(
                "Caps",
                "Enable talent point cap",
                true,
                "When false, boss-tier point caps are off. When true, the other Caps entries apply.");
            EnableEvaSurtrRewards = Config.Bind(
                "EVA Surtr shards",
                "Enable Haldor EVA surtr rewards",
                false,
                "Off by default: the shard items only exist in the CJAYCRAFT modpack, so standalone installs need nothing here. Set true in that pack. When true, selling Cjayride_EvaSurtr_* shards to Haldor applies talent rewards (cap or bonus points).");
            EvaCapPerBoss = Config.Bind(
                "EVA Surtr shards",
                "Cap bonus per Svartalfar Queen or Hel Demon shard",
                9,
                new ConfigDescription(
                    "Raises the active talent limit by this much on the first Haldor sale per character. Repeat sales pay coins only.",
                    new AcceptableValueRange<int>(0, 50)));
            EvaJotunnPointGrant = Config.Bind(
                "EVA Surtr shards",
                "Jotunn shard bonus points",
                15,
                new ConfigDescription(
                    "Adds this many talent points on the first Jotunn (Deep North) shard sale, instead of raising the cap.",
                    new AcceptableValueRange<int>(0, 100)));
            EvaCoinsMist = Config.Bind(
                "EVA Surtr shards",
                "Svartalfar Queen shard coins",
                500,
                new ConfigDescription(
                    "Coins per shard, on the first sale and every repeat sale.",
                    new AcceptableValueRange<int>(0, 100000)));
            EvaCoinsAsh = Config.Bind(
                "EVA Surtr shards",
                "Hel Demon shard coins",
                1000,
                new ConfigDescription(
                    "Coins per shard, on the first sale and every repeat sale.",
                    new AcceptableValueRange<int>(0, 100000)));
            EvaCoinsDeep = Config.Bind(
                "EVA Surtr shards",
                "Jotunn shard coins",
                5000,
                new ConfigDescription(
                    "Coins per shard, on the first sale and every repeat sale.",
                    new AcceptableValueRange<int>(0, 100000)));
            EnableAutoTrim = Config.Bind(
                "Caps",
                "Auto trim over-cap nodes",
                true,
                "When a player spends more talent points than the cap allows, the outer nodes are removed from the tree automatically. Inner nodes are kept. Free respec button still works.");
            EnableEffectScaling = Config.Bind(
                "TalentTree effect scaling",
                "Enable effect scaling",
                false,
                "Scale only M2Valheim TalentTree movement and damage. Nothing else in the game is changed.");
            MovementEffectScale = Config.Bind(
                "TalentTree effect scaling",
                "Movement effect scale",
                0.5f,
                new ConfigDescription(
                    "Multiplier on positive Talent Tree movement modifiers. 1 = Talent Tree default.",
                    new AcceptableValueRange<float>(0f, 1f)));
            DamageEffectScale = Config.Bind(
                "TalentTree effect scaling",
                "Damage effect scale",
                0.5f,
                new ConfigDescription(
                    "Multiplier on Talent Tree damage bonuses. 1 = Talent Tree default.",
                    new AcceptableValueRange<float>(0f, 1f)));
            ParryEffectScale = Config.Bind(
                "Parry",
                "Parry effect scale",
                0.35f,
                new ConfigDescription(
                    "Multiplier on extra parry power (timed-block bonus above 1x), after other mods. 1 = unchanged. 0.35 turns a 2x parry into 1.35x.",
                    new AcceptableValueRange<float>(0f, 1f)));
            Harmony harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(ParryScalePatch));
            TalentTreeEffectScale.Apply(harmony);

            bool budgetOk = TT.Available
                && TT.Type("Core.TalentPointBudgetService") != null
                && TT.Type("Core.TalentRebuildService") != null;
            if (EnableTalentPointCap.Value)
            {
                if (!budgetOk)
                {
                    Logger.LogError("Talent Tree budget types were not found. The point cap is off.");
                }
                else
                {
                    harmony.PatchAll(typeof(BudgetPatch));
                    harmony.PatchAll(typeof(RebuildPatch));
                    harmony.PatchAll(typeof(BossKeyPatch));
                    CapHint.Apply(harmony);
                    Type talentView = TT.Type("UI.TalentWindowView");
                    if (talentView == null || AccessTools.Property(talentView, "PointsText") == null)
                    {
                        Logger.LogWarning("Talent Pace: the Talent Tree point counter element was not found, so the cap text falls back to the window title for placement.");
                    }
                }
            }

            if (EnableEvaSurtrRewards.Value)
            {
                harmony.PatchAll(typeof(EvaSurtrSellPatch));
            }

            new Terminal.ConsoleCommand(
                "talentpace",
                "Print Talent Pace scaling and cap for your character.",
                args =>
                {
                    args.Context.AddString(DescribeState());
                    Player player = Player.m_localPlayer;
                    if (player == null)
                    {
                        args.Context.AddString("Talent Pace: no local player.");
                        return;
                    }

                    if (!EnableTalentPointCap.Value)
                    {
                        args.Context.AddString("Talent cap: off.");
                        return;
                    }

                    Pace.Summary summary = Pace.Describe(player);
                    args.Context.AddString(summary.Line);
                });

            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded. " + DescribeState());
        }

        internal static string DescribeState()
        {
            string scale = EnableEffectScaling.Value
                ? "move " + MovementEffectScale.Value.ToString("0.##") + " damage " + DamageEffectScale.Value.ToString("0.##")
                : "off";
            string cap = EnableTalentPointCap.Value ? "on" : "off";
            string parry = ParryEffectScale.Value.ToString("0.##");
            return "Talent Pace: effect scaling " + scale + ", parry " + parry + ", point cap " + cap + ".";
        }

        internal static void ScaleTimedBlockBonus(ref float timedBlockBonus)
        {
            float scale = ParryEffectScale.Value;
            if (scale >= 0.999f || timedBlockBonus <= 1.0001f)
            {
                return;
            }

            timedBlockBonus = 1f + (timedBlockBonus - 1f) * scale;
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
            Type budget = TT.Type("Core.TalentPointBudgetService");
            return AccessTools.DeclaredMethod(budget, "GetTotalPoints", new[] { typeof(Player), TT.Type("Core.TalentRuntime") });
        }

        private static void Postfix([HarmonyArgument(0)] Player player, ref int __result)
        {
            if (player == null)
            {
                return;
            }

            Pace.RememberNatural(player, __result);
            __result += EvaSurtrRewards.EvaGrantedPoints(player);
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
            Type rebuild = TT.Type("Core.TalentRebuildService");
            return AccessTools.DeclaredMethod(rebuild, "Rebuild", new[] { TT.Type("Core.TalentRuntime") });
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

            Pace.InvalidateStage(__instance);
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
        private static MethodInfo _rebuild;
        private static MethodInfo _commitAndSave;
        private static PropertyInfo _cacheVersion;
        private static MethodInfo _sync;
        private static MethodInfo _getRuntime;
        private static FieldInfo _runtimes;
        private static PropertyInfo _isDirty;
        private static PropertyInfo _playerProp;
        private static PropertyInfo _stateProp;
        private static PropertyInfo _ranksProp;
        private static PropertyInfo _cacheProp;
        private static PropertyInfo _ringProp;
        private static PropertyInfo _tierProp;
        private static bool _reflected;
        private static bool _reflectFailed;

        internal struct Summary
        {
            public string Line;
        }

        internal static int CapFor(Player player)
        {
            return EffectiveCap(player, StageFor(player));
        }

        internal static int EffectiveCap(Player player, int stage)
        {
            int cap = BaseCapFor(player, stage);
            if (cap == int.MaxValue)
            {
                return cap;
            }

            return cap + EvaSurtrRewards.EvaCapBonus(player);
        }

        private static int BaseCapFor(Player player, int stage)
        {
            if (player == null)
            {
                return Plugin.CapNone.Value;
            }

            if (stage >= 7 && Plugin.FaderRemovesCap.Value)
            {
                return int.MaxValue;
            }

            if (stage == 6 && Plugin.QueenRemovesCap.Value)
            {
                return int.MaxValue;
            }

            switch (stage)
            {
                case 0: return Plugin.CapNone.Value;
                case 1: return Plugin.CapEikthyr.Value;
                case 2: return Plugin.CapElder.Value;
                case 3: return Plugin.CapBonemass.Value;
                case 4: return Plugin.CapModer.Value;
                case 5: return Plugin.CapYagluth.Value;
                case 6: return Plugin.CapQueen.Value;
                default: return Plugin.CapFader.Value;
            }
        }

        // Single pass over the boss keys. HaveUniqueKey is a list scan, so this is
        // the only place that touches them. Cached per player until a defeated_ key
        // is added.
        private static int StageFor(Player player)
        {
            if (player == null)
            {
                return 0;
            }

            long id = player.GetPlayerID();
            if (_stage.TryGetValue(id, out int cached))
            {
                return cached;
            }

            int stage = 0;
            if (player.HaveUniqueKey("defeated_fader"))
            {
                _stage[id] = 7;
                return 7;
            }

            if (player.HaveUniqueKey("defeated_queen"))
            {
                _stage[id] = 6;
                return 6;
            }

            if (player.HaveUniqueKey(BossOrder[0])) stage = 1;
            if (player.HaveUniqueKey(BossOrder[1])) stage = 2;
            if (player.HaveUniqueKey(BossOrder[2])) stage = 3;
            if (player.HaveUniqueKey(BossOrder[3])) stage = 4;
            if (player.HaveUniqueKey(BossOrder[4])) stage = 5;
            _stage[id] = stage;
            return stage;
        }

        internal static void InvalidateStage(Player player)
        {
            if (player == null)
            {
                return;
            }

            _stage.Remove(player.GetPlayerID());
            InvalidateHintCache(player);
        }

        internal static void InvalidateHintCache(Player player)
        {
            if (player != null && player == _hintPlayer)
            {
                _hintPlayer = null;
            }
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

        internal static bool NeedsFreeRespec(Player player)
        {
            if (player == null || !Plugin.EnableTalentPointCap.Value)
            {
                return false;
            }

            int cap = CapFor(player);
            if (cap == int.MaxValue)
            {
                return false;
            }

            return CountSpentPoints(player) > cap;
        }

        internal static int CountSpentPoints(Player player)
        {
            if (player == null || !EnsureReflection())
            {
                return 0;
            }

            object runtime = GetRuntime(player);
            if (runtime == null)
            {
                return 0;
            }

            object state = _stateProp.GetValue(runtime, null);
            if (state == null)
            {
                return 0;
            }

            IDictionary ranks = _ranksProp.GetValue(state, null) as IDictionary;
            if (ranks == null || ranks.Count == 0)
            {
                return 0;
            }

            int spent = 0;
            foreach (DictionaryEntry entry in ranks)
            {
                string id = entry.Key as string;
                int rank = entry.Value is int value ? value : 0;
                if (string.IsNullOrEmpty(id) || rank <= 0)
                {
                    continue;
                }

                object def = _tryGetOne(id);
                if (def == null)
                {
                    continue;
                }

                int tier = Convert.ToInt32(_tierProp.GetValue(def, null));
                spent += rank * Cost(tier);
            }

            return spent;
        }

        internal static bool TryFreeRespec(Player player)
        {
            if (player == null || !EnsureReflection())
            {
                return false;
            }

            if (!NeedsFreeRespec(player))
            {
                return false;
            }

            if (TryInvokeTalentRespec(player))
            {
                InvalidateHintCache(player);
                MarkDirty(player);
                Plugin.Log(player.GetPlayerName() + " used a free talent respec (over boss cap).");
                return true;
            }

            object runtime = GetRuntime(player);
            if (runtime == null)
            {
                return false;
            }

            object state = _stateProp.GetValue(runtime, null);
            IDictionary ranks = state == null ? null : _ranksProp.GetValue(state, null) as IDictionary;
            if (ranks == null)
            {
                return false;
            }

            _keys.Clear();
            foreach (DictionaryEntry entry in ranks)
            {
                if (entry.Key is string id)
                {
                    _keys.Add(id);
                }
            }

            for (int i = 0; i < _keys.Count; i++)
            {
                ranks.Remove(_keys[i]);
            }

            _isDirty.SetValue(runtime, true, null);
            Type rebuildType = TT.Type("Core.TalentRebuildService");
            MethodInfo rebuild = rebuildType == null
                ? null
                : AccessTools.DeclaredMethod(rebuildType, "Rebuild", new[] { TT.Type("Core.TalentRuntime") });
            if (rebuild != null)
            {
                rebuild.Invoke(null, new object[] { runtime });
            }

            _sync.Invoke(null, new object[] { runtime });
            InvalidateStage(player);
            Plugin.Log(player.GetPlayerName() + " used a free talent respec (over boss cap).");
            return true;
        }

        private static bool _respecSearched;
        private static MethodInfo _freeRespecMethod;

        private static bool TryInvokeTalentRespec(Player player)
        {
            if (!_respecSearched)
            {
                _respecSearched = true;
                Assembly assembly = TT.Assembly;
                if (assembly != null)
                {
                    Type[] types = assembly.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        Type type = types[i];
                        if (type.FullName == null || type.FullName.IndexOf("Respec", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        MethodInfo method = AccessTools.DeclaredMethod(type, "Respec", new[] { typeof(Player) })
                            ?? AccessTools.DeclaredMethod(type, "TryRespec", new[] { typeof(Player) });
                        if (method != null && method.IsStatic)
                        {
                            _freeRespecMethod = method;
                            break;
                        }
                    }
                }
            }

            if (_freeRespecMethod == null)
            {
                return false;
            }

            try
            {
                object result = _freeRespecMethod.Invoke(null, new object[] { player });
                return result is bool flag ? flag : true;
            }
            catch (Exception ex)
            {
                Plugin.Log("Talent Tree respec invoke failed: " + ex.Message);
                return false;
            }
        }

        private static object GetRuntime(Player player)
        {
            if (_getRuntime == null)
            {
                return null;
            }

            _oneArg[0] = player;
            object runtime = _getRuntime.Invoke(null, _oneArg);
            _oneArg[0] = null;
            return runtime;
        }

        internal static string Hint(Player player)
        {
            int stage = StageFor(player);
            int cap = EffectiveCap(player, stage);
            if (cap == int.MaxValue)
            {
                return "Current Progress: " + StageBossShort(stage) + "\nAll talent point limits are removed.";
            }

            int natural = NaturalFor(player);
            int granted = EvaSurtrRewards.EvaGrantedPoints(player);
            int spent = CountSpentPoints(player);

            if (player == _hintPlayer && stage == _hintStage && natural == _hintNatural && cap == _hintCap && spent == _hintSpent)
            {
                return _hintText;
            }

            _hintPlayer = player;
            _hintStage = stage;
            _hintNatural = natural;
            _hintCap = cap;
            _hintSpent = spent;

            var lines = new System.Text.StringBuilder(256);
            lines.Append("Current Progress: ").Append(StageBossShort(stage)).Append('\n');
            int evaCap = EvaSurtrRewards.EvaCapBonus(player);

            if (stage < NextBoss.Length)
            {
                bool final = stage == NextBoss.Length - 1 && Plugin.FaderRemovesCap.Value;
                lines.Append("To Unlock Points: Kill ").Append(NextBoss[stage]);
                if (final)
                {
                    lines.Append(" (removes all limits)");
                }

                lines.Append('\n');
            }

            lines.Append("Points Earned by XP: ").Append(natural);
            if (granted > 0)
            {
                lines.Append(" (+").Append(granted).Append(" from Jotunn shard)");
            }

            if (natural > cap)
            {
                lines.Append(" (").Append(natural - cap).Append(" saved past the limit)");
            }

            lines.Append('\n');
            lines.Append("Points Spent: ").Append(spent).Append('\n');
            lines.Append("Points Allowed: ").Append(cap);
            if (evaCap > 0)
            {
                lines.Append(" (+").Append(evaCap).Append(" from EVA shards)");
            }

            lines.Append('\n');
            if (stage < NextBoss.Length)
            {
                if (stage == NextBoss.Length - 1 && Plugin.FaderRemovesCap.Value)
                {
                    lines.Append("Kill ").Append(NextBoss[stage]).Append(" to Remove All Limits");
                }
                else
                {
                    int nextCap = NextCapForStage(stage);
                    lines.Append("Kill ").Append(NextBoss[stage]).Append(" to Raise Limit to ").Append(nextCap)
                        .Append(" Points");
                    if (stage == 4 && Plugin.EvaJotunnPointGrant.Value > 0 && Plugin.EnableEvaSurtrRewards.Value)
                    {
                        lines.Append('\n');
                        lines.Append("Selling the Jotunn shard to Haldor grants +").Append(Plugin.EvaJotunnPointGrant.Value)
                            .Append(" talent points");
                    }
                }
            }

            if (NeedsFreeRespec(player))
            {
                lines.Append('\n');
                lines.Append("Spent ").Append(spent).Append(" points, above the ").Append(cap)
                    .Append(" allowed — use Free respec below (no cost)");
            }

            _hintText = lines.ToString();
            return _hintText;
        }

        static int NextCapForStage(int stage)
        {
            switch (stage)
            {
                case 0: return Plugin.CapEikthyr.Value;
                case 1: return Plugin.CapElder.Value;
                case 2: return Plugin.CapBonemass.Value;
                case 3: return Plugin.CapModer.Value;
                case 4: return Plugin.CapYagluth.Value;
                case 5: return Plugin.CapQueen.Value;
                default: return Plugin.CapFader.Value;
            }
        }

        // Short names for the "Current Progress:" line, so it reads "Bonemass" not "After Bonemass".
        static string StageBossShort(int stage)
        {
            switch (stage)
            {
                case 0: return "No boss";
                case 1: return "Eikthyr";
                case 2: return "The Elder";
                case 3: return "Bonemass";
                case 4: return "Moder";
                case 5: return "Yagluth";
                case 6: return "The Queen";
                default: return "The Fader";
            }
        }

        static readonly string[] NextBoss =
        {
            "Eikthyr",
            "the Elder",
            "Bonemass",
            "Moder",
            "Yagluth",
            "the Queen",
            "the Fader"
        };

        static readonly System.Collections.Generic.Dictionary<long, int> _natural = new System.Collections.Generic.Dictionary<long, int>();
        static readonly System.Collections.Generic.Dictionary<long, int> _stage = new System.Collections.Generic.Dictionary<long, int>(8);
        static readonly object[] _oneArg = new object[1];
        static readonly object[] _tryGetArgs = new object[2];
        static Player _hintPlayer;
        static int _hintStage = -1;
        static int _hintNatural = -1;
        static int _hintCap = -1;
        static int _hintSpent = -1;
        static string _hintText;

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
            _oneArg[0] = player;
            _getRuntime.Invoke(null, _oneArg);
            _oneArg[0] = null;
        }

        // Scratch buffers reused across rebuilds. Rebuild is event-driven, not
        // per-frame, so reusing them keeps the cap pass allocation-free in steady state.
        private static readonly System.Collections.Generic.List<Node> _nodes = new System.Collections.Generic.List<Node>(64);
        private static readonly System.Collections.Generic.Dictionary<string, int> _kept = new System.Collections.Generic.Dictionary<string, int>(64);
        private static readonly System.Collections.Generic.List<string> _keys = new System.Collections.Generic.List<string>(64);
        private static readonly Func<string, int> _lookup = id => _kept.TryGetValue(id, out int rank) ? rank : 0;
        private static bool _inTrim;

        internal static void TrimCache(object runtime)
        {
            if (runtime == null || !EnsureReflection())
            {
                return;
            }

            Player player = _playerProp.GetValue(runtime, null) as Player;
            if (player == null)
            {
                return;
            }

            int cap = CapFor(player);
            if (cap == int.MaxValue)
            {
                return;
            }

            object state = _stateProp.GetValue(runtime, null);
            if (state == null)
            {
                return;
            }

            IDictionary ranks = _ranksProp.GetValue(state, null) as IDictionary;
            if (ranks == null || ranks.Count == 0)
            {
                return;
            }

            _nodes.Clear();
            _kept.Clear();
            foreach (DictionaryEntry entry in ranks)
            {
                string id = entry.Key as string;
                int rank = entry.Value is int value ? value : 0;
                if (string.IsNullOrEmpty(id) || rank <= 0)
                {
                    continue;
                }

                object def = _tryGetOne(id);
                if (def == null)
                {
                    continue;
                }

                int ring = Convert.ToInt32(_ringProp.GetValue(def, null));
                // Tier is the NodeTier enum; a boxed enum must not be cast straight to int.
                int tier = Convert.ToInt32(_tierProp.GetValue(def, null));
                _nodes.Add(new Node(id, ring, Cost(tier), rank));
            }

            _nodes.Sort(static (Node a, Node b) =>
            {
                int ring = a.Ring.CompareTo(b.Ring);
                return ring != 0 ? ring : string.CompareOrdinal(a.Id, b.Id);
            });

            int remaining = cap;
            int keptPoints = 0;
            bool trimmed = false;
            for (int i = 0; i < _nodes.Count; i++)
            {
                Node node = _nodes[i];
                int affordable = node.Cost <= 0 ? node.Rank : remaining / node.Cost;
                int apply = Math.Min(node.Rank, Math.Max(0, affordable));
                _kept[node.Id] = apply;
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

            _keys.Clear();
            foreach (DictionaryEntry entry in ranks)
            {
                if (entry.Key is string id)
                {
                    _keys.Add(id);
                }
            }

            object cache = _buildCache.Invoke(null, new object[] { _keys, _lookup });
            _cacheProp.SetValue(runtime, cache, null);
            _sync.Invoke(null, new object[] { runtime });

            if (Plugin.EnableAutoTrim.Value)
            {
                TrimRanksPersistently(runtime, ranks, cap, keptPoints);
            }

            Plugin.Log(player.GetPlayerName() + " talent cap " + cap + ", " + keptPoints + " points active.");
        }

        // Requested behaviour: when the player has spent more points than the cap allows,
        // really remove the nodes (outer rings first) from the saved tree, not only from the
        // effect cache. Inner nodes are kept, and CommitAndSave persists the result.
        private static void TrimRanksPersistently(object runtime, IDictionary ranks, int cap, int keptPoints)
        {
            if (_inTrim)
            {
                return;
            }

            _keys.Clear();
            foreach (DictionaryEntry entry in ranks)
            {
                string id = entry.Key as string;
                int rank = entry.Value is int value ? value : 0;
                if (string.IsNullOrEmpty(id) || rank <= 0)
                {
                    continue;
                }

                _kept.TryGetValue(id, out int keep);
                if (keep >= rank)
                {
                    continue;
                }

                _keys.Add(id);
            }

            if (_keys.Count == 0)
            {
                return;
            }

            _inTrim = true;
            try
            {
                for (int i = 0; i < _keys.Count; i++)
                {
                    string id = _keys[i];
                    _kept.TryGetValue(id, out int keep);
                    if (keep > 0)
                    {
                        ranks[id] = keep;
                    }
                    else
                    {
                        ranks.Remove(id);
                    }
                }

                _isDirty.SetValue(runtime, true, null);
                _cacheVersion?.SetValue(runtime, _cacheVersionInt(runtime) + 1, null);

                if (_commitAndSave != null)
                {
                    _commitAndSave.Invoke(null, new object[] { runtime });
                }
                else if (_rebuild != null)
                {
                    _rebuild.Invoke(null, new object[] { runtime });
                    _sync.Invoke(null, new object[] { runtime });
                }

                Player trimmed = _playerProp.GetValue(runtime, null) as Player;
                if (trimmed != null)
                {
                    InvalidateHintCache(trimmed);
                    Plugin.Log(trimmed.GetPlayerName() + " talent tree trimmed to the " + cap + " point cap (" + keptPoints + " points kept).");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log("Talent trim failed: " + ex.Message);
            }
            finally
            {
                _inTrim = false;
                _keys.Clear();
            }
        }

        static int _cacheVersionInt(object runtime)
        {
            if (_cacheVersion == null)
            {
                return 0;
            }

            object value = _cacheVersion.GetValue(runtime, null);
            return value is int i ? i : 0;
        }

        // TryGet has an out parameter; one reusable args array per call, no boxing beyond the call itself.
        static object _tryGetOne(string id)
        {
            _tryGetArgs[0] = id;
            _tryGetArgs[1] = null;
            if (!(bool)_tryGet.Invoke(null, _tryGetArgs))
            {
                _tryGetArgs[0] = null;
                return null;
            }

            object def = _tryGetArgs[1];
            _tryGetArgs[0] = null;
            _tryGetArgs[1] = null;
            return def;
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
            Type registry = TT.Type("Core.TalentRegistry");
            Type definition = TT.Type("Core.TalentDefinition");
            Type rebuild = TT.Type("Core.TalentRebuildService");
            Type runtime = TT.Type("Core.TalentRuntime");
            Type service = TT.Type("Core.TalentRuntimeService");
            Type passives = TT.Type("Core.TalentPassivesService");
            _tryGet = AccessTools.DeclaredMethod(registry, "TryGet", new[] { typeof(string), definition.MakeByRefType() });
            _buildCache = AccessTools.DeclaredMethod(rebuild, "BuildCache");
            _rebuild = AccessTools.DeclaredMethod(rebuild, "Rebuild", new[] { runtime });
            _commitAndSave = AccessTools.DeclaredMethod(service, "CommitAndSave", new[] { runtime });
            _cacheVersion = AccessTools.DeclaredProperty(runtime, "CacheVersion");
            _sync = AccessTools.DeclaredMethod(passives, "SyncToPlayer", new[] { runtime });
            _getRuntime = AccessTools.DeclaredMethod(service, "Get", new[] { typeof(Player) });
            _runtimes = AccessTools.DeclaredField(service, "_runtimes");
            _isDirty = AccessTools.DeclaredProperty(runtime, "IsDirty");
            _playerProp = AccessTools.DeclaredProperty(runtime, "Player");
            _stateProp = AccessTools.DeclaredProperty(runtime, "State");
            _cacheProp = AccessTools.DeclaredProperty(runtime, "Cache");
            _ranksProp = AccessTools.DeclaredProperty(TT.Type("Core.TalentState"), "Ranks");
            _ringProp = AccessTools.DeclaredProperty(definition, "Ring");
            _tierProp = AccessTools.DeclaredProperty(definition, "Tier");
            _reflectFailed = _tryGet == null || _buildCache == null || _sync == null || _getRuntime == null
                || _runtimes == null || _isDirty == null || _playerProp == null || _stateProp == null
                || _cacheProp == null || _ranksProp == null || _ringProp == null || _tierProp == null;
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

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyTimedBlockBonus))]
    [HarmonyPriority(Priority.Last)]
    internal static class ParryScalePatch
    {
        private static void Postfix(ref float timedBlockBonus)
        {
            Plugin.ScaleTimedBlockBonus(ref timedBlockBonus);
        }
    }
}

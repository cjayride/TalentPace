using System.Reflection;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    internal static class EvaSurtrRewards
    {
        internal const string PrefabMist = "Cjayride_EvaSurtr_Mist";
        internal const string PrefabAsh = "Cjayride_EvaSurtr_Ash";
        internal const string PrefabDeep = "Cjayride_EvaSurtr_Deep";

        private const string KeyCapMist = "talentpace_eva_cap_mist";
        private const string KeyCapAsh = "talentpace_eva_cap_ash";
        private const string KeyPointsDeep = "talentpace_eva_points_deep";

        private static MethodInfo _getSellableItem;

        internal static int EvaCapBonus(Player player)
        {
            if (player == null || !Plugin.EnableTalentPointCap.Value)
            {
                return 0;
            }

            int bonus = 0;
            if (player.HaveUniqueKey(KeyCapMist))
            {
                bonus += Plugin.EvaCapPerBoss.Value;
            }

            if (player.HaveUniqueKey(KeyCapAsh))
            {
                bonus += Plugin.EvaCapPerBoss.Value;
            }

            return bonus;
        }

        internal static int EvaGrantedPoints(Player player)
        {
            if (player == null || !Plugin.EnableTalentPointCap.Value)
            {
                return 0;
            }

            if (!player.HaveUniqueKey(KeyPointsDeep))
            {
                return 0;
            }

            return Plugin.EvaJotunnPointGrant.Value;
        }

        /// <summary>Runs before Traders Extended sell handling. Returns true when this sale was handled.</summary>
        internal static bool TrySell(StoreGui storeGui)
        {
            if (storeGui == null || Player.m_localPlayer == null)
            {
                return false;
            }

            ItemDrop.ItemData item = GetSellableItem(storeGui);
            if (item == null)
            {
                return false;
            }

            string prefab = GetPrefabName(item);
            if (!TryMap(prefab, out EvaSurtrKind kind))
            {
                return false;
            }

            Player player = Player.m_localPlayer;
            bool firstTime = IsFirstReward(player, kind);
            int coins = CoinsFor(kind);

            if (firstTime)
            {
                GrantFirstReward(player, kind);
            }

            if (!RemoveOneFromSellSlot(player, item))
            {
                return false;
            }

            if (coins > 0)
            {
                AddCoins(storeGui, player, item, coins);
            }

            string msg = firstTime ? FirstRewardMessage(kind, coins) : $"Sold for {coins} coins (talent reward already claimed).";
            player.Message(MessageHud.MessageType.TopLeft, msg);
            storeGui.m_sellEffects.Create(storeGui.transform.position, UnityEngine.Quaternion.identity);
            storeGui.m_trader?.OnSold();
            storeGui.FillList();
            return true;
        }

        private static bool IsFirstReward(Player player, EvaSurtrKind kind)
        {
            switch (kind)
            {
                case EvaSurtrKind.Mist:
                    return !player.HaveUniqueKey(KeyCapMist);
                case EvaSurtrKind.Ash:
                    return !player.HaveUniqueKey(KeyCapAsh);
                case EvaSurtrKind.Deep:
                    return !player.HaveUniqueKey(KeyPointsDeep);
                default:
                    return false;
            }
        }

        private static void GrantFirstReward(Player player, EvaSurtrKind kind)
        {
            switch (kind)
            {
                case EvaSurtrKind.Mist:
                    player.AddUniqueKey(KeyCapMist);
                    break;
                case EvaSurtrKind.Ash:
                    player.AddUniqueKey(KeyCapAsh);
                    break;
                case EvaSurtrKind.Deep:
                    player.AddUniqueKey(KeyPointsDeep);
                    break;
            }

            Pace.InvalidateStage(player);
            Pace.MarkDirty(player);
            Pace.InvalidateHintCache(player);
        }

        private static int CoinsFor(EvaSurtrKind kind)
        {
            switch (kind)
            {
                case EvaSurtrKind.Mist:
                    return Plugin.EvaCoinsMist.Value;
                case EvaSurtrKind.Ash:
                    return Plugin.EvaCoinsAsh.Value;
                case EvaSurtrKind.Deep:
                    return Plugin.EvaCoinsDeep.Value;
                default:
                    return 0;
            }
        }

        private static string FirstRewardMessage(EvaSurtrKind kind, int coins)
        {
            int n = Plugin.EvaCapPerBoss.Value;
            int points = Plugin.EvaJotunnPointGrant.Value;
            switch (kind)
            {
                case EvaSurtrKind.Mist:
                case EvaSurtrKind.Ash:
                    return $"Haldor accepts the shard: +{n} talent limit (once per character), {coins} coins.";
                case EvaSurtrKind.Deep:
                    return $"Haldor accepts the shard: +{points} talent points (once per character), {coins} coins.";
                default:
                    return "Shard accepted.";
            }
        }

        private static bool TryMap(string prefab, out EvaSurtrKind kind)
        {
            kind = EvaSurtrKind.None;
            if (string.IsNullOrEmpty(prefab))
            {
                return false;
            }

            if (prefab == PrefabMist)
            {
                kind = EvaSurtrKind.Mist;
                return true;
            }

            if (prefab == PrefabAsh)
            {
                kind = EvaSurtrKind.Ash;
                return true;
            }

            if (prefab == PrefabDeep)
            {
                kind = EvaSurtrKind.Deep;
                return true;
            }

            return false;
        }

        private static string GetPrefabName(ItemDrop.ItemData item)
        {
            if (item.m_dropPrefab != null)
            {
                return item.m_dropPrefab.name;
            }

            return item.m_shared?.m_name ?? string.Empty;
        }

        private static ItemDrop.ItemData GetSellableItem(StoreGui storeGui)
        {
            if (_getSellableItem == null)
            {
                _getSellableItem = AccessTools.Method(typeof(StoreGui), "GetSellableItem");
            }

            return _getSellableItem?.Invoke(storeGui, null) as ItemDrop.ItemData;
        }

        private static bool RemoveOneFromSellSlot(Player player, ItemDrop.ItemData item)
        {
            if (item.m_stack > 1)
            {
                item.m_stack--;
                player.GetInventory().Changed();
                return true;
            }

            return player.GetInventory().RemoveItem(item);
        }

        private static void AddCoins(StoreGui storeGui, Player player, ItemDrop.ItemData sold, int amount)
        {
            ItemDrop coinPrefab = storeGui.m_coinPrefab;
            if (coinPrefab == null || amount <= 0)
            {
                return;
            }

            bool cheated = player.GetInventory().ItemCheated(sold.m_shared.m_name);
            player.GetInventory().AddItem(
                coinPrefab.gameObject.name,
                amount,
                coinPrefab.m_itemData.m_quality,
                coinPrefab.m_itemData.m_variant,
                0L,
                string.Empty,
                cheated);
        }

        private enum EvaSurtrKind
        {
            None,
            Mist,
            Ash,
            Deep
        }
    }

    [HarmonyPatch(typeof(StoreGui), "SellItem")]
    internal static class EvaSurtrSellPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(StoreGui __instance)
        {
            if (!Plugin.EnableEvaSurtrRewards.Value)
            {
                return true;
            }

            return !EvaSurtrRewards.TrySell(__instance);
        }
    }
}

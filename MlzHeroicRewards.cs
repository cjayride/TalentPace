using System.Reflection;
using HarmonyLib;

namespace Cjayride.TalentPace
{
    internal static class MlzHeroicRewards
    {
        internal const string PrefabFrigga = "Cjayride_MLZShard_Frigga";
        internal const string PrefabBalder = "Cjayride_MLZShard_Balder";
        internal const string PrefabKraken = "Cjayride_MLZShard_Kraken";
        internal const string PrefabSurtr = "Cjayride_MLZShard_Surtr";
        internal const string PrefabAshQueen = "Cjayride_MLZShard_AshQueen";

        private const string KeyFrigga = "talentpace_mlz_cap_frigga";
        private const string KeyBalder = "talentpace_mlz_cap_balder";
        private const string KeyKraken = "talentpace_mlz_cap_kraken";
        private const string KeySurtr = "talentpace_mlz_cap_surtr";
        private const string KeyAshQueen = "talentpace_mlz_cap_ashqueen";

        private static MethodInfo _getSellableItem;

        internal static int MlzCapBonus(Player player)
        {
            if (player == null || !Plugin.EnableTalentPointCap.Value)
            {
                return 0;
            }

            int bonus = 0;
            if (player.HaveUniqueKey(KeyFrigga))
            {
                bonus += Plugin.MlzFriggaCapBonus.Value;
            }

            if (player.HaveUniqueKey(KeyBalder))
            {
                bonus += Plugin.MlzBalderCapBonus.Value;
            }

            if (player.HaveUniqueKey(KeyKraken))
            {
                bonus += Plugin.MlzKrakenCapBonus.Value;
            }

            if (player.HaveUniqueKey(KeySurtr))
            {
                bonus += Plugin.MlzSurtrCapBonus.Value;
            }

            if (player.HaveUniqueKey(KeyAshQueen))
            {
                bonus += Plugin.MlzAshQueenCapBonus.Value;
            }

            return bonus;
        }

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
            if (!TryMap(prefab, out MlzHeroicKind kind))
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

        private static bool IsFirstReward(Player player, MlzHeroicKind kind)
        {
            return !player.HaveUniqueKey(UniqueKeyFor(kind));
        }

        private static string UniqueKeyFor(MlzHeroicKind kind)
        {
            switch (kind)
            {
                case MlzHeroicKind.Frigga:
                    return KeyFrigga;
                case MlzHeroicKind.Balder:
                    return KeyBalder;
                case MlzHeroicKind.Kraken:
                    return KeyKraken;
                case MlzHeroicKind.Surtr:
                    return KeySurtr;
                case MlzHeroicKind.AshQueen:
                    return KeyAshQueen;
                default:
                    return string.Empty;
            }
        }

        private static void GrantFirstReward(Player player, MlzHeroicKind kind)
        {
            string key = UniqueKeyFor(kind);
            if (!string.IsNullOrEmpty(key))
            {
                player.AddUniqueKey(key);
            }

            Pace.InvalidateStage(player);
            Pace.MarkDirty(player);
            Pace.InvalidateHintCache(player);
        }

        private static int CapFor(MlzHeroicKind kind)
        {
            switch (kind)
            {
                case MlzHeroicKind.Frigga:
                    return Plugin.MlzFriggaCapBonus.Value;
                case MlzHeroicKind.Balder:
                    return Plugin.MlzBalderCapBonus.Value;
                case MlzHeroicKind.Kraken:
                    return Plugin.MlzKrakenCapBonus.Value;
                case MlzHeroicKind.Surtr:
                    return Plugin.MlzSurtrCapBonus.Value;
                case MlzHeroicKind.AshQueen:
                    return Plugin.MlzAshQueenCapBonus.Value;
                default:
                    return 0;
            }
        }

        private static int CoinsFor(MlzHeroicKind kind)
        {
            switch (kind)
            {
                case MlzHeroicKind.Frigga:
                    return Plugin.MlzFriggaCoins.Value;
                case MlzHeroicKind.Balder:
                    return Plugin.MlzBalderCoins.Value;
                case MlzHeroicKind.Kraken:
                    return Plugin.MlzKrakenCoins.Value;
                case MlzHeroicKind.Surtr:
                    return Plugin.MlzSurtrCoins.Value;
                case MlzHeroicKind.AshQueen:
                    return Plugin.MlzAshQueenCoins.Value;
                default:
                    return 0;
            }
        }

        private static string FirstRewardMessage(MlzHeroicKind kind, int coins)
        {
            int cap = CapFor(kind);
            string boss = BossLabel(kind);
            return $"Haldor accepts the {boss} shard: +{cap} talent limit (once per character), {coins} coins.";
        }

        private static string BossLabel(MlzHeroicKind kind)
        {
            switch (kind)
            {
                case MlzHeroicKind.Frigga:
                    return "Frigga";
                case MlzHeroicKind.Balder:
                    return "Balder";
                case MlzHeroicKind.Kraken:
                    return "Kraken";
                case MlzHeroicKind.Surtr:
                    return "Surtr";
                case MlzHeroicKind.AshQueen:
                    return "Ash Huldra Queen";
                default:
                    return "heroic";
            }
        }

        private static bool TryMap(string prefab, out MlzHeroicKind kind)
        {
            kind = MlzHeroicKind.None;
            if (string.IsNullOrEmpty(prefab))
            {
                return false;
            }

            if (prefab == PrefabFrigga)
            {
                kind = MlzHeroicKind.Frigga;
                return true;
            }

            if (prefab == PrefabBalder)
            {
                kind = MlzHeroicKind.Balder;
                return true;
            }

            if (prefab == PrefabKraken)
            {
                kind = MlzHeroicKind.Kraken;
                return true;
            }

            if (prefab == PrefabSurtr)
            {
                kind = MlzHeroicKind.Surtr;
                return true;
            }

            if (prefab == PrefabAshQueen)
            {
                kind = MlzHeroicKind.AshQueen;
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

        private enum MlzHeroicKind
        {
            None,
            Frigga,
            Balder,
            Kraken,
            Surtr,
            AshQueen
        }
    }

    [HarmonyPatch(typeof(StoreGui), "SellItem")]
    internal static class MlzHeroicSellPatch
    {
        [HarmonyPriority(Priority.First)]
        private static bool Prefix(StoreGui __instance)
        {
            if (!Plugin.EnableMlzHeroicRewards.Value)
            {
                return true;
            }

            return !MlzHeroicRewards.TrySell(__instance);
        }
    }
}
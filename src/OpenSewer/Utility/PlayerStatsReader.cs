using UnityEngine;

namespace OpenSewer.Utility
{
    internal readonly struct PlayerStatsSnapshot
    {
        public readonly StatValue Health;
        public readonly StatValue Hunger;
        public readonly StatValue Thirst;
        public readonly StatValue Depression;
        public readonly StatValue SleepNeed;
        public readonly StatValue WCNeed;
        public readonly StatValue Hygiene;

        public PlayerStatsSnapshot(
            StatValue health,
            StatValue hunger,
            StatValue thirst,
            StatValue depression,
            StatValue sleepNeed,
            StatValue wcNeed,
            StatValue hygiene)
        {
            Health = health;
            Hunger = hunger;
            Thirst = thirst;
            Depression = depression;
            SleepNeed = sleepNeed;
            WCNeed = wcNeed;
            Hygiene = hygiene;
        }
    }

    internal static class PlayerStatsReader
    {
        private const float RefreshIntervalSeconds = 0.25f;
        private const float StatMax = 100f;

        private static bool hasSnapshot;
        private static PlayerStatsSnapshot snapshotCache;
        private static float nextRefreshAtUnscaled;

        public static bool IsReady { get; private set; }

        public static bool TryGetSnapshot(out PlayerStatsSnapshot snapshot)
        {
            if (!hasSnapshot || Time.unscaledTime >= nextRefreshAtUnscaled)
                RefreshSnapshot();

            snapshot = snapshotCache;
            return IsReady;
        }

        private static void RefreshSnapshot()
        {
            nextRefreshAtUnscaled = Time.unscaledTime + RefreshIntervalSeconds;

            if (TryReadSnapshot(out var liveSnapshot))
            {
                snapshotCache = liveSnapshot;
                hasSnapshot = true;
                IsReady = true;
                return;
            }

            IsReady = false;
        }

        private static bool TryReadSnapshot(out PlayerStatsSnapshot snapshot)
        {
            snapshot = default;

            var playerStats = PlayerStats.instance;
            if (playerStats == null)
                return false;

            var health = playerStats.health;
            var hungerAndThirst = playerStats.hungerAndThirst;
            var mentalHealth = playerStats.mentalHealth;
            var tiredness = playerStats.tiredness;
            var hygiene = playerStats.hygiene;

            if (health == null || hungerAndThirst == null || mentalHealth == null || tiredness == null || hygiene == null)
                return false;

            float wcNeed = hungerAndThirst.ToiletNeed;
            if (float.IsNaN(wcNeed) || float.IsInfinity(wcNeed))
                wcNeed = Mathf.Max(hungerAndThirst.Bowel, hungerAndThirst.Bladder);

            snapshot = new PlayerStatsSnapshot(
                ToStat(health.currentStatus),
                ToStat(hungerAndThirst.Hunger),
                ToStat(hungerAndThirst.Thirst),
                ToStat(mentalHealth.currentStatus),
                ToStat(tiredness.currentStatus),
                ToStat(wcNeed),
                ToStat(hygiene.Hygiene)
            );

            return true;
        }

        private static StatValue ToStat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return new StatValue(false, 0f, false, 0f);

            return new StatValue(true, value, true, StatMax);
        }
    }
}

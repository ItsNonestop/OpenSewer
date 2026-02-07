using System;
using System.Collections.Generic;
using UnityEngine;

namespace OpenSewer.Utility
{
    internal sealed class StatFreezer : MonoBehaviour
    {
        private readonly struct StatRange
        {
            public readonly float Min;
            public readonly float Max;

            public StatRange(float min, float max)
            {
                Min = min;
                Max = max;
            }
        }

        public static bool FreezeAllEnabled;
        public static readonly Dictionary<string, bool> FrozenEnabledByKey = new(StringComparer.Ordinal);
        public static readonly Dictionary<string, float> FrozenTargetByKey = new(StringComparer.Ordinal);

        private static readonly Dictionary<string, Action<float>> SettersByKey = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Func<float>> GettersByKey = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, StatRange> RangesByKey = new(StringComparer.Ordinal);
        private static bool mapsInitialized;

        private void Awake()
        {
            EnsureMapsInitialized();
            Plugin.DLog("StatFreezer initialized");
        }

        private void Update()
        {
            EnsureMapsInitialized();

            if (!PlayerStatsAccess.IsReady())
                return;

            foreach (var pair in FrozenEnabledByKey)
            {
                if (!pair.Value)
                    continue;

                if (!SettersByKey.TryGetValue(pair.Key, out var setter))
                    continue;

                float value = GetFrozenTargetValue(pair.Key);
                setter.Invoke(value);
            }
        }

        public static bool GetFrozenEnabled(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            return FrozenEnabledByKey.TryGetValue(key, out var enabled) && enabled;
        }

        public static void SetFrozenEnabled(string key, bool enabled)
        {
            EnsureMapsInitialized();
            if (string.IsNullOrEmpty(key) || !SettersByKey.ContainsKey(key))
                return;

            if (enabled)
            {
                FrozenEnabledByKey[key] = true;
                FrozenTargetByKey[key] = ClampForKey(key, ReadCurrentValue(key));
                return;
            }

            FrozenEnabledByKey[key] = false;
        }

        public static void SetFrozenTarget(string key, float value)
        {
            EnsureMapsInitialized();
            if (string.IsNullOrEmpty(key) || !SettersByKey.ContainsKey(key))
                return;

            FrozenTargetByKey[key] = ClampForKey(key, value);
        }

        public static void EnableFreezeForKeys(IList<string> keys)
        {
            if (keys == null)
                return;

            EnsureMapsInitialized();
            for (int i = 0; i < keys.Count; i++)
            {
                SetFrozenEnabled(keys[i], true);
            }
        }

        public static void DisableAllFrozen()
        {
            FreezeAllEnabled = false;
            if (FrozenEnabledByKey.Count == 0)
                return;

            var keys = new List<string>(FrozenEnabledByKey.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                FrozenEnabledByKey[keys[i]] = false;
            }
        }

        private static float GetFrozenTargetValue(string key)
        {
            if (FrozenTargetByKey.TryGetValue(key, out var target))
                return ClampForKey(key, target);

            float current = ReadCurrentValue(key);
            float clamped = ClampForKey(key, current);
            FrozenTargetByKey[key] = clamped;
            return clamped;
        }

        private static float ReadCurrentValue(string key)
        {
            if (GettersByKey.TryGetValue(key, out var getter))
                return getter.Invoke();

            return 0f;
        }

        private static float ClampForKey(string key, float value)
        {
            if (RangesByKey.TryGetValue(key, out var range))
                return PlayerStatsAccess.ClampRange(value, range.Min, range.Max);

            return value;
        }

        private static void EnsureMapsInitialized()
        {
            if (mapsInitialized)
                return;

            AddBoundStat("vitals.health", PlayerStatsAccess.GetHealth, PlayerStatsAccess.SetHealth, 0f, 100f);
            AddBoundStat("needs.hunger", PlayerStatsAccess.GetHunger, PlayerStatsAccess.SetHunger, 0f, 100f);
            AddBoundStat("needs.thirst", PlayerStatsAccess.GetThirst, PlayerStatsAccess.SetThirst, 0f, 100f);
            AddBoundStat("needs.sleep", PlayerStatsAccess.GetSleepNeed, PlayerStatsAccess.SetSleepNeed, 0f, 100f);
            AddBoundStat("needs.wc", PlayerStatsAccess.GetWcNeed, PlayerStatsAccess.SetWcNeed, 0f, 100f);
            AddBoundStat("needs.hygiene", PlayerStatsAccess.GetHygiene, PlayerStatsAccess.SetHygiene, 0f, 100f);
            AddBoundStat("mental.depression", PlayerStatsAccess.GetDepression, PlayerStatsAccess.SetDepression, 0f, 100f);

            AddBoundStat("addictions.need.mushroom", PlayerStatsAccess.GetMushroomNeed, PlayerStatsAccess.SetMushroomNeed, 0f, 100f);
            AddBoundStat("addictions.need.alcohol", PlayerStatsAccess.GetAlcoholNeed, PlayerStatsAccess.SetAlcoholNeed, 0f, 100f);
            AddBoundStat("addictions.need.smoking", PlayerStatsAccess.GetSmokingNeed, PlayerStatsAccess.SetSmokingNeed, 0f, 100f);
            AddBoundStat("addictions.need.gambling", PlayerStatsAccess.GetGamblingNeed, PlayerStatsAccess.SetGamblingNeed, 0f, 100f);
            AddBoundStat("addictions.level.mushroom", PlayerStatsAccess.GetMushroomAddiction, PlayerStatsAccess.SetMushroomAddiction, 0f, 100f);
            AddBoundStat("addictions.level.alcohol", PlayerStatsAccess.GetAlcoholAddiction, PlayerStatsAccess.SetAlcoholAddiction, 0f, 100f);
            AddBoundStat("addictions.level.smoking", PlayerStatsAccess.GetSmokingAddiction, PlayerStatsAccess.SetSmokingAddiction, 0f, 100f);
            AddBoundStat("addictions.level.gambling", PlayerStatsAccess.GetGamblingAddiction, PlayerStatsAccess.SetGamblingAddiction, 0f, 100f);

            AddBoundStat("advanced.rate.need.mushroom", PlayerStatsAccess.GetMushroomNeedBaserate, PlayerStatsAccess.SetMushroomNeedBaserate, -1000f, 1000f);
            AddBoundStat("advanced.rate.need.alcohol", PlayerStatsAccess.GetAlcoholNeedBaserate, PlayerStatsAccess.SetAlcoholNeedBaserate, -1000f, 1000f);
            AddBoundStat("advanced.rate.need.smoking", PlayerStatsAccess.GetSmokingNeedBaserate, PlayerStatsAccess.SetSmokingNeedBaserate, -1000f, 1000f);
            AddBoundStat("advanced.rate.need.gambling", PlayerStatsAccess.GetGamblingNeedBaserate, PlayerStatsAccess.SetGamblingNeedBaserate, -1000f, 1000f);
            AddBoundStat("advanced.rate.change.mushroom", PlayerStatsAccess.GetMushroomAddictionChange, PlayerStatsAccess.SetMushroomAddictionChange, -1000f, 1000f);
            AddBoundStat("advanced.rate.change.alcohol", PlayerStatsAccess.GetAlcoholAddictionChange, PlayerStatsAccess.SetAlcoholAddictionChange, -1000f, 1000f);
            AddBoundStat("advanced.rate.change.smoking", PlayerStatsAccess.GetSmokingAddictionChange, PlayerStatsAccess.SetSmokingAddictionChange, -1000f, 1000f);
            AddBoundStat("advanced.rate.change.gambling", PlayerStatsAccess.GetGamblingAddictionChange, PlayerStatsAccess.SetGamblingAddictionChange, -1000f, 1000f);
            AddBoundStat("advanced.withdrawal.mental", PlayerStatsAccess.GetWithdrawalMentalHealthLoss, PlayerStatsAccess.SetWithdrawalMentalHealthLoss, -1000f, 1000f);

            mapsInitialized = true;
        }

        private static void AddBoundStat(string key, Func<float> getter, Action<float> setter, float min, float max)
        {
            SettersByKey[key] = setter;
            GettersByKey[key] = getter;
            RangesByKey[key] = new StatRange(min, max);
        }
    }
}

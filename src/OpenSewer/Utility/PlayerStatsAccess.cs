using UnityEngine;

namespace OpenSewer.Utility
{
    internal static class PlayerStatsAccess
    {
        public const float DefaultMin = 0f;
        public const float DefaultMax = 100f;
        public const float AdvancedMin = -1000f;
        public const float AdvancedMax = 1000f;

        public static bool IsReady()
        {
            return TryGet(out _);
        }

        public static bool TryGet(out PlayerStats playerStats)
        {
            playerStats = PlayerStats.instance;
            if (playerStats == null)
                return false;

            return playerStats.health != null
                && playerStats.hungerAndThirst != null
                && playerStats.mentalHealth != null
                && playerStats.tiredness != null
                && playerStats.hygiene != null
                && playerStats.addictions != null;
        }

        public static float Clamp01Hundred(float value) => Mathf.Clamp(value, DefaultMin, DefaultMax);
        public static float ClampAdvanced(float value) => Mathf.Clamp(value, AdvancedMin, AdvancedMax);
        public static float ClampRange(float value, float min, float max) => Mathf.Clamp(value, min, max);

        public static float GetHealth() => TryGet(out var ps) ? ps.health.currentStatus : 0f;
        public static void SetHealth(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.health.currentStatus = Clamp01Hundred(value);
        }

        public static float GetHunger() => TryGet(out var ps) ? ps.hungerAndThirst.Hunger : 0f;
        public static void SetHunger(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.hungerAndThirst.Hunger = Clamp01Hundred(value);
        }

        public static float GetThirst() => TryGet(out var ps) ? ps.hungerAndThirst.Thirst : 0f;
        public static void SetThirst(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.hungerAndThirst.Thirst = Clamp01Hundred(value);
        }

        public static float GetDepression() => TryGet(out var ps) ? ps.mentalHealth.currentStatus : 0f;
        public static void SetDepression(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.mentalHealth.currentStatus = Clamp01Hundred(value);
        }

        public static float GetSleepNeed() => TryGet(out var ps) ? ps.tiredness.currentStatus : 0f;
        public static void SetSleepNeed(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.tiredness.currentStatus = Clamp01Hundred(value);
        }

        public static float GetWcNeed() => TryGet(out var ps) ? ps.hungerAndThirst.ToiletNeed : 0f;
        public static void SetWcNeed(float value)
        {
            if (!TryGet(out var ps)) return;

            float clamped = Clamp01Hundred(value);
            ps.hungerAndThirst.Bowel = clamped;
            ps.hungerAndThirst.Bladder = clamped;
        }

        public static float GetHygiene() => TryGet(out var ps) ? ps.hygiene.Hygiene : 0f;
        public static void SetHygiene(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.hygiene.Hygiene = Clamp01Hundred(value);
        }

        public static float GetMushroomNeed() => TryGet(out var ps) ? ps.addictions.MushroomNeed : 0f;
        public static void SetMushroomNeed(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.MushroomNeed = Clamp01Hundred(value);
        }

        public static float GetAlcoholNeed() => TryGet(out var ps) ? ps.addictions.AlcoholNeed : 0f;
        public static void SetAlcoholNeed(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.AlcoholNeed = Clamp01Hundred(value);
        }

        public static float GetSmokingNeed() => TryGet(out var ps) ? ps.addictions.SmokingNeed : 0f;
        public static void SetSmokingNeed(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.SmokingNeed = Clamp01Hundred(value);
        }

        public static float GetGamblingNeed() => TryGet(out var ps) ? ps.addictions.GamblingNeed : 0f;
        public static void SetGamblingNeed(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.GamblingNeed = Clamp01Hundred(value);
        }

        public static float GetMushroomAddiction() => TryGet(out var ps) ? ps.addictions.MushroomAddiction : 0f;
        public static void SetMushroomAddiction(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.MushroomAddiction = Clamp01Hundred(value);
        }

        public static float GetAlcoholAddiction() => TryGet(out var ps) ? ps.addictions.AlcoholAddiction : 0f;
        public static void SetAlcoholAddiction(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.AlcoholAddiction = Clamp01Hundred(value);
        }

        public static float GetSmokingAddiction() => TryGet(out var ps) ? ps.addictions.SmokingAddiction : 0f;
        public static void SetSmokingAddiction(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.SmokingAddiction = Clamp01Hundred(value);
        }

        public static float GetGamblingAddiction() => TryGet(out var ps) ? ps.addictions.GamblingAddiction : 0f;
        public static void SetGamblingAddiction(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.GamblingAddiction = Clamp01Hundred(value);
        }

        public static float GetMushroomNeedBaserate() => TryGet(out var ps) ? ps.addictions.MushroomNeedBaserate : 0f;
        public static void SetMushroomNeedBaserate(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.MushroomNeedBaserate = ClampAdvanced(value);
        }

        public static float GetAlcoholNeedBaserate() => TryGet(out var ps) ? ps.addictions.AlcoholNeedBaserate : 0f;
        public static void SetAlcoholNeedBaserate(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.AlcoholNeedBaserate = ClampAdvanced(value);
        }

        public static float GetSmokingNeedBaserate() => TryGet(out var ps) ? ps.addictions.SmokingNeedBaserate : 0f;
        public static void SetSmokingNeedBaserate(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.SmokingNeedBaserate = ClampAdvanced(value);
        }

        public static float GetGamblingNeedBaserate() => TryGet(out var ps) ? ps.addictions.GamblingNeedBaserate : 0f;
        public static void SetGamblingNeedBaserate(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.GamblingNeedBaserate = ClampAdvanced(value);
        }

        public static float GetMushroomAddictionChange() => TryGet(out var ps) ? ps.addictions.MushroomAddictionChange : 0f;
        public static void SetMushroomAddictionChange(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.MushroomAddictionChange = ClampAdvanced(value);
        }

        public static float GetAlcoholAddictionChange() => TryGet(out var ps) ? ps.addictions.AlcoholAddictionChange : 0f;
        public static void SetAlcoholAddictionChange(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.AlcoholAddictionChange = ClampAdvanced(value);
        }

        public static float GetSmokingAddictionChange() => TryGet(out var ps) ? ps.addictions.SmokingAddictionChange : 0f;
        public static void SetSmokingAddictionChange(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.SmokingAddictionChange = ClampAdvanced(value);
        }

        public static float GetGamblingAddictionChange() => TryGet(out var ps) ? ps.addictions.GamblingAddictionChange : 0f;
        public static void SetGamblingAddictionChange(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.GamblingAddictionChange = ClampAdvanced(value);
        }

        public static float GetWithdrawalMentalHealthLoss() => TryGet(out var ps) ? ps.addictions.WithdrawalsymptomMentalhealthLoss : 0f;
        public static void SetWithdrawalMentalHealthLoss(float value)
        {
            if (!TryGet(out var ps)) return;
            ps.addictions.WithdrawalsymptomMentalhealthLoss = ClampAdvanced(value);
        }
    }
}

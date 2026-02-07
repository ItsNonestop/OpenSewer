namespace OpenSewer.Utility
{
    internal readonly struct StatValue
    {
        public readonly bool HasValue;
        public readonly float Value;
        public readonly bool HasMax;
        public readonly float Max;

        public StatValue(bool hasValue, float value, bool hasMax, float max)
        {
            HasValue = hasValue;
            Value = value;
            HasMax = hasMax;
            Max = max;
        }
    }

    internal readonly struct StatsSnapshot
    {
        public readonly StatValue Health;
        public readonly StatValue Hunger;
        public readonly StatValue Thirst;
        public readonly StatValue Depression;
        public readonly StatValue SleepNeed;
        public readonly StatValue WcNeed;
        public readonly StatValue Hygiene;

        public StatsSnapshot(
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
            WcNeed = wcNeed;
            Hygiene = hygiene;
        }

        public bool HasAny =>
            Health.HasValue ||
            Hunger.HasValue ||
            Thirst.HasValue ||
            Depression.HasValue ||
            SleepNeed.HasValue ||
            WcNeed.HasValue ||
            Hygiene.HasValue;
    }
}

using UnityEngine;

namespace OpenSewer.Utility
{
    internal static class TimeAccess
    {
        public static bool IsReady()
        {
            return TryGet(out _);
        }

        public static bool TryGet(out TimeOfDayAzure tod)
        {
            tod = TimeOfDayAzure.instance;
            return tod != null;
        }

        public static int GetDay()
        {
            if (!TryGet(out var tod))
                return 1;

            return ClampDay(tod.GetDay());
        }

        public static void SetDay(int day)
        {
            if (!TryGet(out var tod))
                return;

            int clampedDay = ClampDay(day);
            tod.SetDay(clampedDay);

            // Keep exposed day counters coherent for consumers that read fields directly.
            tod.dayCounter = clampedDay;
            if (tod.currentTimeAndDay != null)
            {
                tod.currentTimeAndDay.day = clampedDay;
                tod.currentTimeAndDay.currentDay = clampedDay;
            }
        }

        public static void GetHourMinute(out int hour, out int minute)
        {
            if (!TryGet(out var tod))
            {
                hour = 0;
                minute = 0;
                return;
            }

            hour = ClampHour(Mathf.FloorToInt(tod.CurrentHours));
            minute = ClampMinute(Mathf.FloorToInt(tod.CurrentMinutes));
        }

        public static void SetHourMinute(int hour, int minute)
        {
            if (!TryGet(out var tod))
                return;

            int clampedHour = ClampHour(hour);
            int clampedMinute = ClampMinute(minute);
            float timelineInHours = clampedHour + (clampedMinute / 60f);

            // Azure timeline uses hour-of-day units (0..24), not normalized 0..1.
            tod.SetTimeline(timelineInHours);

            if (tod.currentTimeAndDay != null)
            {
                tod.currentTimeAndDay.hours = clampedHour;
                tod.currentTimeAndDay.minutes = clampedMinute;
                tod.currentTimeAndDay.seconds = 0f;
            }
        }

        public static void GetSnapshot(out int day, out int hour, out int minute)
        {
            day = GetDay();
            GetHourMinute(out hour, out minute);
        }

        public static int ClampDay(int day)
        {
            return Mathf.Max(1, day);
        }

        public static int ClampHour(int hour)
        {
            return Mathf.Clamp(hour, 0, 23);
        }

        public static int ClampMinute(int minute)
        {
            return Mathf.Clamp(minute, 0, 59);
        }
    }
}

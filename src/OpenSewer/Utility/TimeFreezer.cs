using UnityEngine;

namespace OpenSewer.Utility
{
    internal sealed class TimeFreezer : MonoBehaviour
    {
        public static bool FreezeEnabled;
        public static int FrozenDay = 1;
        public static int FrozenHour;
        public static int FrozenMinute;

        public static void CaptureCurrent()
        {
            if (!TimeAccess.IsReady())
                return;

            TimeAccess.GetSnapshot(out FrozenDay, out FrozenHour, out FrozenMinute);
            FrozenDay = TimeAccess.ClampDay(FrozenDay);
            FrozenHour = TimeAccess.ClampHour(FrozenHour);
            FrozenMinute = TimeAccess.ClampMinute(FrozenMinute);
        }

        public static void SetTarget(int day, int hour, int minute)
        {
            FrozenDay = TimeAccess.ClampDay(day);
            FrozenHour = TimeAccess.ClampHour(hour);
            FrozenMinute = TimeAccess.ClampMinute(minute);
        }

        private void LateUpdate()
        {
            if (!FreezeEnabled)
                return;

            if (!TimeAccess.IsReady())
                return;

            TimeAccess.SetDay(FrozenDay);
            TimeAccess.SetHourMinute(FrozenHour, FrozenMinute);
        }
    }
}

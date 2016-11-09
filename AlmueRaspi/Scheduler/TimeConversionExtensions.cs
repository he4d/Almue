using System;

namespace AlmueRaspi.Scheduler
{
    public static class TimeConversionExtensions
    {
        public static TimeSpan LocalToUtc(this TimeSpan timeSpan)
        {
            var dtNow = DateTime.Now;
            var dt = new DateTime(dtNow.Year, dtNow.Month, dtNow.Day, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
            return TimeZoneInfo.ConvertTimeToUtc(dt).TimeOfDay;
        }
    }
}

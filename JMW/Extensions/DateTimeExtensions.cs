using System;
using System.Globalization;

namespace JMW.Extensions;

public static class DateTimeExtensions
{
    public static string GetElapsedTime(this DateTime curr, DateTime time)
    {
        return new TimeSpan(curr.Ticks - time.Ticks).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }
}
namespace FinTrack.Api.Endpoints;

internal static class DateRanges
{
    public static (DateOnly Start, DateOnly End) Month(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return (start, new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
    }
}

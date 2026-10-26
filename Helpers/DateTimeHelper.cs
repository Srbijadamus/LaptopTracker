namespace LaptopTracker.Helpers;

// Informational lease state derived from Lease/DaaS End Date Planned.
public enum LeaseStatus { None, EndsSoon, Expired }

public static class DateTimeHelper
{
    // How far ahead a lease end still counts as "ends soon".
    public const int LeaseEndsSoonDays = 90;

    private static readonly TimeZoneInfo _cet =
        TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");

    public static DateTime ToCet(this DateTime utcDt)
    {
        var dt = DateTime.SpecifyKind(utcDt, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(dt, _cet);
    }

    // Lease/DaaS end date is edited as a date only. Legacy rows may carry a time
    // component, so a save that leaves the date untouched must not silently drop it:
    // keep the stored value when the date is unchanged, otherwise take the new date
    // (time 00:00:00). Cleared input means NULL.
    public static DateTime? MergeEditedDate(DateTime? stored, DateTime? posted)
    {
        if (posted is null) return null;
        if (stored is not null && stored.Value.Date == posted.Value.Date) return stored;
        return posted.Value.Date;
    }

    // Purely informational classification, compared on the date part only against the
    // server's local today (the same clock DateTime.Today gives the rest of the app).
    // NULL, or an end date further ahead than LeaseEndsSoonDays, yields None.
    public static LeaseStatus ClassifyLease(DateTime? leaseEnd) =>
        ClassifyLease(leaseEnd, DateTime.Today);

    public static LeaseStatus ClassifyLease(DateTime? leaseEnd, DateTime today)
    {
        if (leaseEnd is null) return LeaseStatus.None;

        var end = leaseEnd.Value.Date;
        var reference = today.Date;

        if (end < reference) return LeaseStatus.Expired;
        return end <= reference.AddDays(LeaseEndsSoonDays) ? LeaseStatus.EndsSoon : LeaseStatus.None;
    }
}

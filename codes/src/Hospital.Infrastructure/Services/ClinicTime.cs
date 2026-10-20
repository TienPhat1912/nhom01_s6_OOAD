namespace Hospital.Infrastructure.Services;

public sealed class ClinicTime(TimeProvider provider)
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public DateTime UtcNow => provider.GetUtcNow().UtcDateTime;
    public DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, Zone);
    public DateOnly Today => DateOnly.FromDateTime(LocalNow);
    public DateTime ToUtc(DateOnly date, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(time, DateTimeKind.Unspecified), Zone);
}

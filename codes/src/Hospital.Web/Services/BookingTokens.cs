using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Hospital.Web.Services;

public sealed class BookingTokens(IDataProtectionProvider provider)
{
    public string Protect<T>(T value) => provider.CreateProtector("Hospital.Booking.v1", typeof(T).Name)
        .ToTimeLimitedDataProtector().Protect(JsonSerializer.Serialize(value), TimeSpan.FromMinutes(30));

    public T Read<T>(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 16000) throw new CryptographicException();
        try
        {
            return JsonSerializer.Deserialize<T>(provider.CreateProtector("Hospital.Booking.v1", typeof(T).Name)
                .ToTimeLimitedDataProtector().Unprotect(token)) ?? throw new CryptographicException();
        }
        catch (JsonException ex) { throw new CryptographicException("Invalid booking data.", ex); }
    }
}

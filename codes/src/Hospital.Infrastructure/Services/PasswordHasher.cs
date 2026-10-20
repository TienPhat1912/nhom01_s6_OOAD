using Hospital.Application.Common.Interfaces;

namespace Hospital.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            throw new System.ComponentModel.DataAnnotations.ValidationException("Mật khẩu tối đa 72 byte UTF-8.");
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash)
            || System.Text.Encoding.UTF8.GetByteCount(password) > 72)
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}

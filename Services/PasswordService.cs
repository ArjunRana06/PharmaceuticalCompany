using Microsoft.AspNetCore.Identity;
using Project.Models;

namespace Project.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password) =>
        _hasher.HashPassword(user, password);

    public bool Verify(User user, string password, out bool needsRehash)
    {
        needsRehash = false;

        if (string.IsNullOrEmpty(user.Password) || string.IsNullOrEmpty(password))
        {
            return false;
        }

        if (user.Password.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            var result = _hasher.VerifyHashedPassword(user, user.Password, password);
            needsRehash = result == PasswordVerificationResult.SuccessRehashNeeded;
            return result != PasswordVerificationResult.Failed;
        }

        if (string.Equals(user.Password, password, StringComparison.Ordinal))
        {
            needsRehash = true;
            return true;
        }

        return false;
    }
}

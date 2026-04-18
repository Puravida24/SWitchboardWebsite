using System.Text;
using Isopoh.Cryptography.Argon2;
using Microsoft.AspNetCore.Identity;

namespace TheSwitchboard.Web.Services;

/// <summary>
/// IPasswordHasher implementation using Argon2id (OWASP Password Storage Cheat Sheet 2023+).
/// Hashes produced by this hasher begin with "$argon2id$" — legacy Identity PBKDF2 hashes
/// (which begin with base64-decoded bytes starting with 0x00 or 0x01) are transparently
/// verified and re-hashed on next successful sign-in via the "Rehash" return value.
/// </summary>
public sealed class Argon2IdPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    // OWASP 2023 recommended parameters for Argon2id (minimum)
    // time cost = 3, memory = 64 MiB, parallelism = 1
    private const int TimeCost = 3;
    private const int MemoryCostKiB = 65536; // 64 MiB
    private const int Parallelism = 1;
    private const int HashLength = 32;
    private const int SaltLength = 16;

    // Fallback to the built-in PBKDF2 hasher when verifying legacy hashes.
    private readonly PasswordHasher<TUser> _legacy = new();

    public string HashPassword(TUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,        // Argon2id
            Version = Argon2Version.Nineteen,          // v1.3
            TimeCost = TimeCost,
            MemoryCost = MemoryCostKiB,
            Lanes = Parallelism,
            Threads = Parallelism,
            Password = Encoding.UTF8.GetBytes(password),
            Salt = GenerateSalt(SaltLength),
            HashLength = HashLength
        };
        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        return config.EncodeString(hash.Buffer);
    }

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (hashedPassword.StartsWith("$argon2", StringComparison.Ordinal))
        {
            var ok = Argon2.Verify(hashedPassword, providedPassword);
            return ok ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }

        // Legacy PBKDF2 hash — verify with the default hasher and flag for rehash on success.
        var legacyResult = _legacy.VerifyHashedPassword(user, hashedPassword, providedPassword);
        return legacyResult switch
        {
            PasswordVerificationResult.Success => PasswordVerificationResult.SuccessRehashNeeded,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
            _ => PasswordVerificationResult.Failed
        };
    }

    private static byte[] GenerateSalt(int length)
    {
        var salt = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        return salt;
    }
}

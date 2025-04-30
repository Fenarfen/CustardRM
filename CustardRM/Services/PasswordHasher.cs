using CustardRM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CustardRM.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int Iterations = 100000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public (string hash, string salt) HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new Exception("Error when hashing password: Password cannot be empty");
        }

        var saltBytes = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }

        var keyBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );

        string saltBase64 = Convert.ToBase64String(saltBytes);
        string keyBase64 = Convert.ToBase64String(keyBytes);

        return (keyBase64, saltBase64);
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        var saltBytes = Convert.FromBase64String(storedSalt);

        var keyToCheck = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize
        );

        var keyBase64 = Convert.ToBase64String(keyToCheck);

        return keyBase64 == storedHash;
    }
}

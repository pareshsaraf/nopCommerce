using System.Security.Cryptography;
using System.Text;
using Nop.Core;

namespace Nop.Plugin.Misc.PunchOut.Services;

/// <summary>
/// Hashes PunchOut shared secrets the same way they are stored on a PunchOut identity.
/// </summary>
public static class PunchOutSharedSecret
{
    /// <summary>
    /// Creates the stored form of a shared secret
    /// </summary>
    /// <param name="sharedSecret">Shared secret</param>
    /// <returns>SHA-256 hash</returns>
    public static string Hash(string sharedSecret)
    {
        return HashHelper.CreateHash(Encoding.UTF8.GetBytes(sharedSecret), "SHA256");
    }

    /// <summary>
    /// Checks a presented shared secret against the stored value.
    /// The stored value is a SHA-256 hash. Identity edits previously saved the secret in plaintext, and those rows still match.
    /// </summary>
    /// <param name="storedValue">Stored shared-secret hash, or a legacy plaintext secret</param>
    /// <param name="sharedSecret">Presented shared secret</param>
    /// <returns>True when the secret matches</returns>
    public static bool Matches(string storedValue, string sharedSecret)
    {
        if (string.IsNullOrEmpty(storedValue) || string.IsNullOrEmpty(sharedSecret))
            return false;

        if (FixedTimeEquals(Hash(sharedSecret), storedValue))
            return true;

        // A hash-shaped stored value must not authenticate as itself. Only unhashed legacy secrets take this path.
        return !IsStoredHash(storedValue) && FixedTimeEquals(sharedSecret, storedValue);
    }

    /// <summary>
    /// Returns the value to persist after an identity edit.
    /// The admin grid posts the current stored value back when the secret was not changed.
    /// </summary>
    /// <param name="storedValue">Current stored value</param>
    /// <param name="submittedSecret">Value posted from the admin grid or form</param>
    /// <returns>Hash to store</returns>
    public static string ResolveStoredHash(string storedValue, string submittedSecret)
    {
        if (!string.IsNullOrEmpty(submittedSecret) &&
            !string.Equals(storedValue, submittedSecret, StringComparison.Ordinal))
            return Hash(submittedSecret);

        if (!string.IsNullOrEmpty(storedValue) && !IsStoredHash(storedValue))
            return Hash(storedValue);

        return storedValue;
    }

    /// <summary>
    /// Gets a value indicating whether the stored value has the shape of a SHA-256 hash produced by <see cref="Hash"/>
    /// </summary>
    /// <param name="storedValue">Stored value</param>
    /// <returns>True when the value is 64 uppercase hex characters</returns>
    public static bool IsStoredHash(string storedValue)
    {
        if (string.IsNullOrEmpty(storedValue) || storedValue.Length != 64)
            return false;

        foreach (var c in storedValue)
        {
            var isHex = c is >= '0' and <= '9' or >= 'A' and <= 'F';
            if (!isHex)
                return false;
        }

        return true;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        if (leftBytes.Length != rightBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

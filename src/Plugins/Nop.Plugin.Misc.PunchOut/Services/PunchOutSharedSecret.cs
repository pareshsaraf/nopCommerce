using System.Security.Cryptography;
using System.Text;
using Nop.Core;

namespace Nop.Plugin.Misc.PunchOut.Services;

/// <summary>
/// Hashes PunchOut shared secrets and compares them in constant time.
/// </summary>
public static class PunchOutSharedSecret
{
    /// <summary>
    /// SHA-256 hex length produced by <see cref="HashHelper.CreateHash"/>.
    /// </summary>
    public const int Sha256HexLength = 64;

    /// <summary>
    /// Creates the stored form of a shared secret.
    /// </summary>
    /// <param name="sharedSecret">Shared secret supplied by an administrator or a cXML request</param>
    /// <returns>Uppercase SHA-256 hex digest</returns>
    public static string Hash(string sharedSecret)
    {
        ArgumentException.ThrowIfNullOrEmpty(sharedSecret);

        return HashHelper.CreateHash(Encoding.UTF8.GetBytes(sharedSecret), "SHA256");
    }

    /// <summary>
    /// Gets a value indicating whether the stored value is already a SHA-256 hex digest.
    /// </summary>
    /// <param name="storedSecret">Value persisted in <c>SharedSecretHash</c></param>
    /// <returns><c>true</c> when every character is a 64-digit hex digest</returns>
    public static bool IsHashed(string storedSecret)
    {
        if (string.IsNullOrEmpty(storedSecret) || storedSecret.Length != Sha256HexLength)
            return false;

        foreach (var ch in storedSecret)
        {
            var isHex = ch is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
            if (!isHex)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks an incoming shared secret against the stored value.
    /// </summary>
    /// <param name="storedSecret">Value persisted in <c>SharedSecretHash</c></param>
    /// <param name="incomingSecret">Shared secret from the cXML request</param>
    /// <returns><c>true</c> when the incoming secret matches</returns>
    /// <remarks>
    /// New identities store a SHA-256 digest, so the incoming secret is hashed before comparison.
    /// Older rows saved by the identity editor may still hold the secret in plaintext; those match directly
    /// and are rewritten as a digest on the next successful setup or edit.
    /// </remarks>
    public static bool Matches(string storedSecret, string incomingSecret)
    {
        if (string.IsNullOrEmpty(storedSecret) || string.IsNullOrEmpty(incomingSecret))
            return false;

        if (IsHashed(storedSecret))
            return FixedTimeEquals(storedSecret, Hash(incomingSecret));

        return FixedTimeEquals(storedSecret, incomingSecret);
    }

    /// <summary>
    /// Returns the value that should be persisted after an identity edit.
    /// </summary>
    /// <param name="currentStored">Current persisted secret</param>
    /// <param name="submittedSecret">Value posted from the admin grid</param>
    /// <returns>Digest to store</returns>
    /// <remarks>
    /// The grid shows the persisted digest. Saving a row without changing that cell must not hash the digest again.
    /// A new secret, or an unchanged legacy plaintext value, is hashed before it is stored.
    /// </remarks>
    public static string NormalizeForStorage(string currentStored, string submittedSecret)
    {
        if (string.IsNullOrEmpty(submittedSecret))
            return currentStored;

        if (IsHashed(currentStored) && string.Equals(currentStored, submittedSecret, StringComparison.Ordinal))
            return currentStored;

        return Hash(submittedSecret);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        // FixedTimeEquals throws when the buffers differ in length.
        if (leftBytes.Length != rightBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

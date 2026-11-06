using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Misc.PunchOut;

/// <summary>
/// Picks the customer email a PunchOut setup may use.
/// </summary>
public static class PunchOutCustomerEmails
{
    /// <summary>
    /// Local-part marker for an account created because the contact email belongs to a store customer.
    /// </summary>
    public const string LocalPartMarker = "+punchout";

    /// <summary>
    /// Selects an email that is not owned by a store account.
    /// </summary>
    /// <param name="contactEmail">Contact email from the setup request</param>
    /// <param name="contactIsStoreAccount">Whether a non-PunchOut customer already uses <paramref name="contactEmail"/></param>
    /// <param name="dedicatedIsStoreAccount">Whether a non-PunchOut customer already uses the dedicated alias</param>
    /// <param name="hashedIsStoreAccount">Whether a non-PunchOut customer already uses the hashed alias</param>
    /// <returns>Email of the PunchOut customer record to reuse or create</returns>
    public static string Choose(string contactEmail, bool contactIsStoreAccount, bool dedicatedIsStoreAccount, bool hashedIsStoreAccount)
    {
        if (!contactIsStoreAccount)
            return contactEmail;

        if (!dedicatedIsStoreAccount)
            return Dedicated(contactEmail);

        if (!hashedIsStoreAccount)
            return DedicatedWithDiscriminator(contactEmail);

        throw new InvalidOperationException("PunchOut contact email cannot be mapped without using a store account.");
    }

    /// <summary>
    /// Builds a stable alias that does not match the contact email.
    /// </summary>
    /// <param name="contactEmail">Contact email</param>
    /// <returns>Dedicated PunchOut email</returns>
    public static string Dedicated(string contactEmail)
    {
        var (local, domain) = Split(contactEmail);
        return $"{local}{LocalPartMarker}@{domain}";
    }

    /// <summary>
    /// Builds a second stable alias when the dedicated alias is itself a store account.
    /// </summary>
    /// <param name="contactEmail">Contact email</param>
    /// <returns>Dedicated PunchOut email with a hash discriminator</returns>
    public static string DedicatedWithDiscriminator(string contactEmail)
    {
        var (local, domain) = Split(contactEmail);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contactEmail.Trim().ToLowerInvariant())))[..8].ToLowerInvariant();
        return $"{local}{LocalPartMarker}.{hash}@{domain}";
    }

    private static (string local, string domain) Split(string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new ArgumentException("Contact email is required.", nameof(contactEmail));

        var at = contactEmail.LastIndexOf('@');
        if (at <= 0 || at == contactEmail.Length - 1)
            throw new ArgumentException("Contact email is not valid.", nameof(contactEmail));

        return (contactEmail[..at], contactEmail[(at + 1)..]);
    }
}

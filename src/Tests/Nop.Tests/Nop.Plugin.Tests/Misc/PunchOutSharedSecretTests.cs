using AwesomeAssertions;
using Nop.Plugin.Misc.PunchOut.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.Misc;

[TestFixture]
public class PunchOutSharedSecretTests
{
    [Test]
    public void MatchesAcceptsOriginalSecretAgainstStoredHash()
    {
        var storedHash = PunchOutSharedSecret.Hash("s3cret");

        PunchOutSharedSecret.IsStoredHash(storedHash).Should().BeTrue();
        PunchOutSharedSecret.Matches(storedHash, "s3cret").Should().BeTrue();
        PunchOutSharedSecret.Matches(storedHash, "wrong").Should().BeFalse();
        PunchOutSharedSecret.Matches(storedHash, storedHash).Should().BeFalse();
        PunchOutSharedSecret.Matches(storedHash, null).Should().BeFalse();
        PunchOutSharedSecret.Matches(null, "s3cret").Should().BeFalse();
    }

    [Test]
    public void MatchesAcceptsLegacyPlaintextSecretButNotAHashPresentedAsTheSecret()
    {
        PunchOutSharedSecret.Matches("s3cret", "s3cret").Should().BeTrue();
        PunchOutSharedSecret.Matches("s3cret", "other").Should().BeFalse();
    }

    [Test]
    public void ResolveStoredHashKeepsHashAndRehashesPlaintextOrNewSecrets()
    {
        var storedHash = PunchOutSharedSecret.Hash("s3cret");

        PunchOutSharedSecret.ResolveStoredHash(storedHash, storedHash).Should().Be(storedHash);
        PunchOutSharedSecret.ResolveStoredHash(storedHash, string.Empty).Should().Be(storedHash);
        PunchOutSharedSecret.ResolveStoredHash(storedHash, null).Should().Be(storedHash);
        PunchOutSharedSecret.ResolveStoredHash(storedHash, "new-secret").Should().Be(PunchOutSharedSecret.Hash("new-secret"));
        PunchOutSharedSecret.ResolveStoredHash("s3cret", "s3cret").Should().Be(storedHash);
        PunchOutSharedSecret.ResolveStoredHash("s3cret", null).Should().Be(storedHash);
    }
}

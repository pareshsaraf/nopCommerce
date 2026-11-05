using AwesomeAssertions;
using Nop.Plugin.Misc.PunchOut.Services;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.PunchOut;

[TestFixture]
public class PunchOutSharedSecretTests
{
    private const string Secret = "punchout-shared-secret";

    [Test]
    public void HashMatchesOriginalSecretAndNotTheDigest()
    {
        var digest = PunchOutSharedSecret.Hash(Secret);

        PunchOutSharedSecret.IsHashed(digest).Should().BeTrue();
        PunchOutSharedSecret.Matches(digest, Secret).Should().BeTrue();
        PunchOutSharedSecret.Matches(digest, digest).Should().BeFalse();
        PunchOutSharedSecret.Matches(digest, "wrong-secret").Should().BeFalse();
    }

    [Test]
    public void LegacyPlaintextStillMatchesAndDifferentLengthsDoNotThrow()
    {
        PunchOutSharedSecret.IsHashed(Secret).Should().BeFalse();
        PunchOutSharedSecret.Matches(Secret, Secret).Should().BeTrue();
        PunchOutSharedSecret.Matches(Secret, "other-secret").Should().BeFalse();
        PunchOutSharedSecret.Matches(Secret, new string('x', PunchOutSharedSecret.Sha256HexLength)).Should().BeFalse();
    }

    [Test]
    public void NormalizeKeepsUnchangedDigestAndHashesNewOrPlaintextValues()
    {
        var digest = PunchOutSharedSecret.Hash(Secret);

        PunchOutSharedSecret.NormalizeForStorage(digest, digest).Should().Be(digest);
        PunchOutSharedSecret.NormalizeForStorage(digest, "replacement-secret").Should().Be(PunchOutSharedSecret.Hash("replacement-secret"));
        PunchOutSharedSecret.NormalizeForStorage(Secret, Secret).Should().Be(digest);
        PunchOutSharedSecret.NormalizeForStorage(digest, null).Should().Be(digest);
    }
}

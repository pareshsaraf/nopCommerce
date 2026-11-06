using AwesomeAssertions;
using Nop.Plugin.Misc.PunchOut;
using NUnit.Framework;

namespace Nop.Tests.Nop.Plugin.Tests.PunchOut;

[TestFixture]
public class PunchOutCustomerEmailsTests
{
    [Test]
    public void ChooseKeepsTheContactEmailWhenNoStoreAccountOwnsIt()
    {
        PunchOutCustomerEmails.Choose("buyer@example.com", false, false, false)
            .Should().Be("buyer@example.com");
    }

    [Test]
    public void ChooseUsesADedicatedAliasWhenTheContactEmailIsAStoreAccount()
    {
        PunchOutCustomerEmails.Choose("buyer@example.com", true, false, false)
            .Should().Be("buyer+punchout@example.com");
    }

    [Test]
    public void ChooseUsesAHashedAliasWhenTheDedicatedAliasIsAlsoAStoreAccount()
    {
        var email = PunchOutCustomerEmails.Choose("buyer@example.com", true, true, false);

        email.Should().Be(PunchOutCustomerEmails.DedicatedWithDiscriminator("buyer@example.com"));
        email.Should().NotBe("buyer@example.com");
        email.Should().NotBe("buyer+punchout@example.com");
    }

    [Test]
    public void ChooseRefusesToFallBackOntoAStoreAccount()
    {
        var act = () => PunchOutCustomerEmails.Choose("buyer@example.com", true, true, true);

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void DedicatedAliasIsStable()
    {
        PunchOutCustomerEmails.Dedicated("Buyer@Example.com")
            .Should().Be(PunchOutCustomerEmails.Dedicated("Buyer@Example.com"));
        PunchOutCustomerEmails.DedicatedWithDiscriminator("Buyer@Example.com")
            .Should().Be(PunchOutCustomerEmails.DedicatedWithDiscriminator("Buyer@Example.com"));
    }
}

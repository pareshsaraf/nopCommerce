using AwesomeAssertions;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Customers;

[TestFixture]
public class CustomerServiceTests : ServiceTest
{
    private ICustomerService _customerService;

    [SetUp]
    public async Task SetUp()
    {
        _customerService = GetService<ICustomerService>();
    }

    [Test]
    public async Task CanCheckIsInCustomerRole()
    {
        var customer = await _customerService.GetCustomerByEmailAsync(NopTestsDefaults.AdminEmail);

        var isInCustomerRole = await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.AdministratorsRoleName, false);
        isInCustomerRole.Should().BeTrue();
        isInCustomerRole = await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.AdministratorsRoleName);
        isInCustomerRole.Should().BeTrue();
        isInCustomerRole = await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.GuestsRoleName, false);
        isInCustomerRole.Should().BeFalse();
        isInCustomerRole = await _customerService.IsInCustomerRoleAsync(customer, NopCustomerDefaults.GuestsRoleName);
        isInCustomerRole.Should().BeFalse();
    }

    [Test]
    public async Task CanCheckWhetherCustomerIsAdmin()
    {
        var customer = await _customerService.GetCustomerByEmailAsync(NopTestsDefaults.AdminEmail);
        var isAdmin = await _customerService.IsAdminAsync(customer);
        isAdmin.Should().BeTrue();
    }

    [Test]
    public async Task CanCheckWhetherCustomerIsGuest()
    {
        var customer = await _customerService.GetCustomerByEmailAsync("builtin@search_engine_record.com");
        var isGuest = await _customerService.IsGuestAsync(customer);
        isGuest.Should().BeTrue();
    }

    [Test]
    public async Task CanCheckWhetherCustomerIsRegistered()
    {
        var customer = await _customerService.GetCustomerByEmailAsync(NopTestsDefaults.AdminEmail);

        var isRegistered = await _customerService.IsRegisteredAsync(customer);
        isRegistered.Should().BeTrue();
    }

    [Test]
    public async Task CanRemoveAddressAssignedAsBillingAddress()
    {
        var customer = await _customerService.GetCustomerByEmailAsync(NopTestsDefaults.AdminEmail);
        var addresses = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);

        addresses.Count.Should().Be(1);

        var address = addresses[0];

        await _customerService.InsertCustomerAddressAsync(customer, address);

        var addressesByCustomer = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);
        addressesByCustomer.Count.Should().Be(1);

        var billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
        billingAddress.Should().NotBeNull();

        billingAddress = await _customerService.GetCustomerBillingAddressAsync(customer);
        billingAddress.Id.Should().Be(address.Id);

        await _customerService.RemoveCustomerAddressAsync(customer, address);

        addressesByCustomer = await _customerService.GetAddressesByCustomerIdAsync(customer.Id);
        var countAddresses = addressesByCustomer.Count;

        var billingAddressId = customer.BillingAddressId;

        await _customerService.InsertCustomerAddressAsync(customer, address);
        customer.BillingAddressId = address.Id;

        countAddresses.Should().Be(0);
        billingAddressId.Should().BeNull();
    }

    [Test]
    public async Task OtpUsesTheSignedInCustomerInsteadOfAnOlderUnverifiedPhone()
    {
        var phone = "+1999" + DateTime.UtcNow.Ticks;

        var earlier = await CreateRegisteredCustomerAsync("otp-earlier-" + Guid.NewGuid().ToString("N") + "@example.com", phone, false);
        var current = await CreateRegisteredCustomerAsync("otp-current-" + Guid.NewGuid().ToString("N") + "@example.com", phone, false);

        try
        {
            var registrationCustomer = await _customerService.GetOtpCustomerAsync(current, phone, forLogin: false);
            registrationCustomer.Id.Should().Be(current.Id);

            var loginCustomer = await _customerService.GetOtpCustomerAsync(null, phone, forLogin: true);
            loginCustomer.Should().BeNull();

            current.PhoneSmsVerified = true;
            await _customerService.UpdateCustomerAsync(current);

            loginCustomer = await _customerService.GetOtpCustomerAsync(null, phone, forLogin: true);
            loginCustomer.Id.Should().Be(current.Id);

            var otherAccount = await _customerService.GetOtpCustomerAsync(earlier, phone, forLogin: false);
            otherAccount.Id.Should().Be(earlier.Id);
        }
        finally
        {
            await DeleteCustomerAsync(current);
            await DeleteCustomerAsync(earlier);
        }
    }

    private async Task<Customer> CreateRegisteredCustomerAsync(string email, string phone, bool phoneVerified)
    {
        var customer = new Customer
        {
            Email = email,
            Username = email,
            Active = true,
            CreatedOnUtc = DateTime.UtcNow,
            Phone = phone,
            PhoneSmsVerified = phoneVerified
        };

        await _customerService.InsertCustomerAsync(customer);

        var registeredRole = await _customerService.GetCustomerRoleBySystemNameAsync(NopCustomerDefaults.RegisteredRoleName);
        await _customerService.AddCustomerRoleMappingAsync(new CustomerCustomerRoleMapping
        {
            CustomerId = customer.Id,
            CustomerRoleId = registeredRole.Id
        });

        return customer;
    }

    private async Task DeleteCustomerAsync(Customer customer)
    {
        customer.Email = string.Empty;
        customer.Username = string.Empty;
        customer.Phone = null;
        customer.Active = false;
        await _customerService.UpdateCustomerAsync(customer);
        await _customerService.DeleteCustomerAsync(customer);
    }
}
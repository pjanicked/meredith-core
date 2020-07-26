namespace WhyNotEarth.Meredith.Platform.Subscriptions
{
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using WhyNotEarth.Meredith.Data.Entity;
    using WhyNotEarth.Meredith.Data.Entity.Models.Modules.Platform;
    using WhyNotEarth.Meredith.Exceptions;
    using WhyNotEarth.Meredith.Services;

    public class CustomerService
    {
        public CustomerService(
            MeredithDbContext meredithDbContext,
            IStripeCustomerService stripeCustomerService)
        {
            _meredithDbContext = meredithDbContext;
            _stripeCustomerService = stripeCustomerService;
        }

        private readonly MeredithDbContext _meredithDbContext;

        private readonly IStripeCustomerService _stripeCustomerService;

        public async Task<Customer> AddCustomerAsync(int tenantId)
        {
            var tenant = await _meredithDbContext.Tenants
                .Include(t => t.Owner)
                .FirstOrDefaultAsync(t => t.Id == tenantId);
            if (tenant == null)
            {
                throw new RecordNotFoundException($"Tenant {tenantId} not found");
            }

            var stripeCustomerId = await _stripeCustomerService.AddCustomerAsync(tenant.Owner.Email, tenant.Owner.FullName);
            var customer = new Customer
            {
                TenantId = tenant.Id,
                StripeId = stripeCustomerId,
            };
            _meredithDbContext.Add(customer);
            await _meredithDbContext.SaveChangesAsync();
            return customer;
        }

        public async Task<Card> AddCardAsync(int tenantId, string? token)
        {
            var customer = await _meredithDbContext.PlatformCustomers
                .FirstOrDefaultAsync(c => c.TenantId == tenantId);
            if (customer == null)
            {
                throw new RecordNotFoundException($"Customer with tenant ID {tenantId} not found");
            }

            var cardId = await _stripeCustomerService.AddCardAsync(customer.StripeId, token);
            var card = new Card
            {
                StripeId = cardId,
                CustomerId = customer.Id
            };
            _meredithDbContext.Add(card);
            await _meredithDbContext.SaveChangesAsync();
            return card;
        }

        public async Task DeleteCardAsync(int cardId)
        {
            var card = await _meredithDbContext.PlatformCards
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.Id == cardId);
            await _stripeCustomerService.DeleteCardAsync(card.Customer!.StripeId, card.StripeId);
        }
    }
}
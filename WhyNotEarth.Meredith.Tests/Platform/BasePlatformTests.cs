namespace WhyNotEarth.Meredith.Tests.Platform
{
    using System.Linq;
    using System.Threading.Tasks;
    using Faker;
    using Microsoft.Extensions.DependencyInjection;
    using WhyNotEarth.Meredith.Services;
    using WhyNotEarth.Meredith.Tests.Data;

    public class BasePlatformTests : DatabaseContextTest
    {
        public BasePlatformTests()
        {
        }

        protected async Task<Meredith.Data.Entity.Models.Tenant> CreateTenant()
        {
            var tenant = new Meredith.Data.Entity.Models.Tenant
            {
                Owner = new Meredith.Data.Entity.Models.User
                {
                    FirstName = Name.First(),
                    LastName = Name.Last(),
                    Email = Internet.Email(),
                }
            };
            DbContext.Add(tenant);
            await DbContext.SaveChangesAsync();
            return tenant;
        }

        protected async Task<Meredith.Data.Entity.Models.Modules.Platform.Plan> GetPlan(string planName)
        {
            return DbContext.PlatformPlans.FirstOrDefault(p => p.Name == planName);
        }
    }
}
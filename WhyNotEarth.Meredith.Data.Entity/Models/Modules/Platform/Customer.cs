namespace WhyNotEarth.Meredith.Data.Entity.Models.Modules.Platform
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class Customer
    {
        public int Id { get; set; }

        public string StripeId { get; set; } = null!;

        public Tenant Tenant { get; set; } = null!;

        public int TenantId { get; set; }
    }

    public class CustomerConfig : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            builder.ToTable("Customers", "Platform");
        }
    }
}
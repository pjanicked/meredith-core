using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhyNotEarth.Meredith.Data.Entity.Models.Modules.Platform
{
    public class Plan
    {
        public int Id { get; set; }

        public string StripeId { get; set; } = null!;

        public string Name { get; set; } = null!;
    }

    public class PlanConfig : IEntityTypeConfiguration<Plan>
    {
        public void Configure(EntityTypeBuilder<Plan> builder)
        {
            builder.ToTable("Plans", "Platform");
        }
    }
}
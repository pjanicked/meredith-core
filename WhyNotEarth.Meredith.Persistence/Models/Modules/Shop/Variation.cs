using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhyNotEarth.Meredith.Persistence.Models.Modules.Shop
{
    public class Variation
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public string Name { get; set; } = null!;

        public int PriceId { get; set; }

        public Price Price { get; set; } = null!;
    }

    public class VariationEntityConfig : IEntityTypeConfiguration<Variation>
    {
        public void Configure(EntityTypeBuilder<Variation> builder)
        {
            builder.ToTable("Variations", "ModuleShop");
            builder.Property(b => b.Name).IsRequired();
        }
    }
}
namespace WhyNotEarth.Meredith.Data.Entity.Models.Modules.Platform
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class Card
    {
        public int Id { get; set; }

        public string StripeId { get; set; } = null!;

        public Customer? Customer { get; set; } = null!;

        public int CustomerId { get; set; }
    }

    public class CardConfig : IEntityTypeConfiguration<Card>
    {
        public void Configure(EntityTypeBuilder<Card> builder)
        {
            builder.ToTable("Cards", "Platform");
        }
    }
}
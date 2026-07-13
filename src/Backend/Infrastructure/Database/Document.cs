namespace Backend.Infrastructure.Database;

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Content).IsRequired();
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasOne(d => d.Customer)
            .WithMany()
            .HasForeignKey(d => d.CustomerId);

        builder.HasOne(d => d.Supplier)
            .WithMany()
            .HasForeignKey(d => d.SupplierId);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Documents_Owner",
            "(CustomerId IS NOT NULL AND SupplierId IS NULL) OR (CustomerId IS NULL AND SupplierId IS NOT NULL)"));
    }
}

class DocumentSeeding : SeedEntity<BackendContext, Document>
{
    readonly List<int> customerIds;
    readonly List<int> supplierIds;

    public DocumentSeeding(BackendContext context) : base(context)
    {
        customerIds = context.Customers.Select(c => c.Id).ToList();
        supplierIds = context.Suppliers.Select(s => s.Id).ToList();
    }

    protected override IEnumerable<Document> GetSeedItems()
    {
        var faker = new Faker("it");

        for (var i = 0; i < 50; i++)
        {
            var attachToCustomer = customerIds.Count > 0 &&
                (supplierIds.Count == 0 || faker.Random.Bool());

            yield return new Document
            {
                Title = faker.Random.Bool()
                    ? $"{faker.Lorem.Sentence(3)}.md"
                    : $"{faker.Lorem.Sentence(3)}.txt",
                Content = faker.Lorem.Paragraphs(faker.Random.Int(1, 3)),
                CustomerId = attachToCustomer ? faker.PickRandom(customerIds) : null,
                SupplierId = attachToCustomer ? null : faker.PickRandom(supplierIds),
                UploadedAt = faker.Date.Past(1).ToUniversalTime(),
            };
        }
    }
}

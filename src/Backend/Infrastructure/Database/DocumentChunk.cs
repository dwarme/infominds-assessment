namespace Backend.Infrastructure.Database;

public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    /// <summary>0-based order of this chunk within the parent document.</summary>
    public int ChunkIndex { get; set; }

    public string Text { get; set; } = "";

    /// <summary>JSON-serialized float[] embedding vector.</summary>
    public string EmbeddingJson { get; set; } = "[]";

    // Why denormalize owner IDs: copy CustomerId/SupplierId from Document so RAG search can
    // filter by owner (e.g. "chunks for customer X", "which suppliers mention…") without joining
    // Documents on every query. Kept in sync when the indexer writes chunks.
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
}

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ChunkIndex).IsRequired();
        builder.Property(c => c.Text).IsRequired();
        builder.Property(c => c.EmbeddingJson).IsRequired();

        builder.HasOne(c => c.Document)
            .WithMany()
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.DocumentId);
        builder.HasIndex(c => c.CustomerId);
        builder.HasIndex(c => c.SupplierId);
        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex }).IsUnique();

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_DocumentChunks_Owner",
            "(CustomerId IS NOT NULL AND SupplierId IS NULL) OR (CustomerId IS NULL AND SupplierId IS NOT NULL)"));
    }
}

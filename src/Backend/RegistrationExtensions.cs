namespace Backend;

using Backend.Features.Rag;

static class RegistrationExtensions
{
    public static void UseSwaggerDocumentation(this WebApplication app)
    {
        // Map Swagger to the custom url api-docs/swagger
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    public static void InitAndSeedBackendContest(this WebApplication app)
    {
        // Make sure, that the database exists
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<BackendContext>();
        context.Database.EnsureCreated();
        EnsureDocumentsTable(context);
        EnsureDocumentChunksTable(context);

        if (app.Environment.IsDevelopment())
        {
            context.Seed();
            BackfillRagIndex(scope.ServiceProvider);
        }
    }

    static void BackfillRagIndex(IServiceProvider services)
    {
        var indexer = services.GetRequiredService<DocumentIndexer>();
        indexer.IndexUnindexedDocumentsAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    static void EnsureDocumentsTable(BackendContext context)
    {
        // EnsureCreated() skips schema updates on an existing database.
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "Documents" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Documents" PRIMARY KEY AUTOINCREMENT,
                "Title" TEXT NOT NULL,
                "Content" TEXT NOT NULL,
                "CustomerId" INTEGER NULL,
                "SupplierId" INTEGER NULL,
                "UploadedAt" TEXT NOT NULL,
                CONSTRAINT "FK_Documents_Customers_CustomerId" FOREIGN KEY ("CustomerId") REFERENCES "Customers" ("Id"),
                CONSTRAINT "FK_Documents_Suppliers_SupplierId" FOREIGN KEY ("SupplierId") REFERENCES "Suppliers" ("Id"),
                CONSTRAINT "CK_Documents_Owner" CHECK (
                    (CustomerId IS NOT NULL AND SupplierId IS NULL) OR
                    (CustomerId IS NULL AND SupplierId IS NOT NULL)
                )
            );
            """);
    }

    static void EnsureDocumentChunksTable(BackendContext context)
    {
        // EnsureCreated() skips schema updates on an existing database.
        context.Database.ExecuteSqlRaw("""
            CREATE TABLE IF NOT EXISTS "DocumentChunks" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_DocumentChunks" PRIMARY KEY AUTOINCREMENT,
                "DocumentId" INTEGER NOT NULL,
                "ChunkIndex" INTEGER NOT NULL,
                "Text" TEXT NOT NULL,
                "EmbeddingJson" TEXT NOT NULL,
                "CustomerId" INTEGER NULL,
                "SupplierId" INTEGER NULL,
                CONSTRAINT "FK_DocumentChunks_Documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES "Documents" ("Id") ON DELETE CASCADE,
                CONSTRAINT "CK_DocumentChunks_Owner" CHECK (
                    (CustomerId IS NOT NULL AND SupplierId IS NULL) OR
                    (CustomerId IS NULL AND SupplierId IS NOT NULL)
                )
            );
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_DocumentId" ON "DocumentChunks" ("DocumentId");
            """);
        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_CustomerId" ON "DocumentChunks" ("CustomerId");
            """);
        context.Database.ExecuteSqlRaw("""
            CREATE INDEX IF NOT EXISTS "IX_DocumentChunks_SupplierId" ON "DocumentChunks" ("SupplierId");
            """);
        context.Database.ExecuteSqlRaw("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentChunks_DocumentId_ChunkIndex"
            ON "DocumentChunks" ("DocumentId", "ChunkIndex");
            """);
    }
}

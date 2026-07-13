namespace Backend;

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

        if (app.Environment.IsDevelopment())
            context.Seed();
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
}

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

        if (app.Environment.IsDevelopment())
            context.Seed();
    }
}

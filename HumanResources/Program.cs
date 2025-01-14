using HumanResources;
using HumanResources.Data;
using HumanResources.Domain;
using Serilog;

await WithSeriLog(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddSerilog("HumanResources");

    builder.Services
        .AddCustomCors()
        .AddHttpContextAccessor()
        .AddEndpointsApiExplorer()
        .AddCustomMediatR(new[] {typeof(Employee)})
        .AddCustomValidators(new[] {typeof(Employee)})
        .AddPersistence("northwind_db", builder.Configuration)
        .AddSwaggerGen()
        .AddSchemeRegistry(builder.Configuration)
        .AddDaprClient();

    var app = builder.Build();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
    }

    app.UseSerilogRequestLogging();

    app.MapGet("/error", () => Results.Problem("An error occurred.", statusCode: 500))
        .ExcludeFromDescription();

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseCustomCors();
    app.UseRouting();

    app.UseSwagger();
    app.UseSwaggerUI();

    await app.DoDbMigrationAsync(app.Logger);
    await app.DoSeedData(app.Logger);

    app.Run();
});

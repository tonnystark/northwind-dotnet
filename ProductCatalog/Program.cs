using ProductCatalog;
using ProductCatalog.Data;
using ProductCatalog.Domain;
using ProductCatalog.UseCases;
using Serilog;

await WithSeriLog(async () =>
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.AddSerilog("ProductCatalog");

    builder.Services
        .AddCustomCors()
        .AddEndpointsApiExplorer()
        .AddHttpContextAccessor()
        .AddCustomMediatR(new[] {typeof(Product)})
        .AddCustomValidators(new[] {typeof(Product)})
        .AddPersistence("northwind_db", builder.Configuration)
        .AddSwaggerGen()
        .AddSchemeRegistry(builder.Configuration)
        .AddCdCConsumers()
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

    app.MapGet("/api/v1/products",
        async ([FromHeader(Name = "x-query")] string xQuery, HttpContext httpContext, ISender sender) =>
        {
            var queryModel = httpContext.SafeGetListQuery<GetProducts.Query, ListResultModel<ProductDto>>(xQuery);
            return await sender.Send(queryModel);
        });

    app.MapPost("/api/v1/products",
        async (MutateProduct.CreateCommand command, ISender sender) => await sender.Send(command));

    app.MapPut("/api/v1/products/{id}",
        async (Guid id, MutateProduct.UpdateCommand command, ISender sender) =>
            await sender.Send(command with {Id = id}));

    app.MapDelete("/api/v1/products/{id}",
        async (Guid id, ISender sender) => await sender.Send(new MutateProduct.DeleteCommand {Id = id}));

    app.MapFallback(() => Results.Redirect("/swagger"));

    app.Run();
});

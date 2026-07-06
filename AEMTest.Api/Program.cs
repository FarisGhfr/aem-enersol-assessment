using AEMTest.Api.Data;
using AEMTest.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ─────────────────────────────────────────────────────────────────

// Entity Framework Core with SQL Server LocalDB (Code First)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// HttpClient for remote API calls
builder.Services.AddHttpClient<IRemoteApiService, RemoteApiService>();

// Business logic services
builder.Services.AddScoped<ISyncService, SyncService>();

// Controllers
builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "AEM Assessment — Platform & Well Sync API",
        Version = "v1",
        Description = "Syncs Platform and Well data from the AEM remote REST API into a local SQL Server LocalDB database."
    });
    // Include XML comments from controller/model summaries
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ─── Auto-migrate Database on Startup ─────────────────────────────────────────
// Applies any pending EF Core migrations automatically.
// This ensures LocalDB is always up-to-date without manual intervention.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Database migration applied successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations.");
        throw;
    }
}

// ─── Middleware Pipeline ───────────────────────────────────────────────────────

// Always expose Swagger (not just in Development, for easy assessment review)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "AEM Assessment API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger at root URL
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Logger.LogInformation("AEM Assessment API is starting. Navigate to / for Swagger UI.");

app.Run();

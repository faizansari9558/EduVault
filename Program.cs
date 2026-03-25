using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SmartELibrary.Data;
using SmartELibrary.Filters;
using SmartELibrary.Services;

// Load .env file before configuration is built so env vars override appsettings.json
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var mvcBuilder = builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var rawConnectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration["MONGODB_URI"];
    if (string.IsNullOrEmpty(rawConnectionString))
        throw new InvalidOperationException(
            "MongoDB connection string is not configured. "
            + "Set ConnectionStrings__DefaultConnection or MONGODB_URI as an environment variable.");
    var databaseName =
        builder.Configuration["Database:Name"]
        ?? builder.Configuration["MONGODB_DATABASE"]
        ?? "EduVaultDB";
    options.UseMongoDB(rawConnectionString, databaseName);
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddScoped<ITeacherCodeGenerator, TeacherCodeGenerator>();
builder.Services.AddScoped<IStudentSemesterApprovalService, StudentSemesterApprovalService>();
builder.Services.AddScoped<StudentSemesterApprovalFilter>();
builder.Services.AddSingleton<IStudentSessionTracker, StudentSessionTracker>();
builder.Services.AddSingleton<IMongoSequenceService, MongoSequenceService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var sequenceService = scope.ServiceProvider.GetRequiredService<IMongoSequenceService>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseStartup");
    try
    {
        var runMigrations = args.Any(a => string.Equals(a, "--migrate", StringComparison.OrdinalIgnoreCase));
        if (!runMigrations)
        {
            var migrateOnStartup = app.Configuration["MIGRATE_ON_STARTUP"];
            runMigrations = string.Equals(migrateOnStartup, "true", StringComparison.OrdinalIgnoreCase);
        }
        if (runMigrations)
        {
            // MongoDB doesn't use relational migrations. 
            // We can add index creation here if needed.
            logger.LogInformation("Automatic migrations are not supported for MongoDB. Skipping.");
        }
        else
        {
            logger.LogInformation("Skipping automatic migrations. Pass --migrate to apply DB updates.");
        }

        var wipeUserGenerated = args.Any(a => string.Equals(a, "--wipe-user-generated", StringComparison.OrdinalIgnoreCase));
        var wipeAllData = args.Any(a => string.Equals(a, "--wipe-all-data", StringComparison.OrdinalIgnoreCase));
        var noSeed = args.Any(a => string.Equals(a, "--no-seed", StringComparison.OrdinalIgnoreCase));

        logger.LogInformation("Connected to MongoDB.");
        logger.LogInformation("Wipe flags: userGenerated={WipeUserGenerated}, allData={WipeAllData}, noSeed={NoSeed}", wipeUserGenerated, wipeAllData, noSeed);

        if (wipeAllData)
        {
            logger.LogWarning("Wiping ALL application tables...");

            var usersBefore = await db.Users.CountAsync();
            logger.LogWarning("Before wipe: Users={Users}", usersBefore);

            await DatabaseWipeService.WipeAllApplicationDataAsync(db);

            var usersAfter = await db.Users.CountAsync();
            logger.LogWarning("After wipe: Users={Users}", usersAfter);
        }
        else if (wipeUserGenerated)
        {
            logger.LogWarning("Wiping user-generated content tables...");

            var materialsBefore = await db.Materials.CountAsync();
            var pagesBefore = await db.MaterialPages.CountAsync();
            logger.LogWarning("Before wipe: Materials={Materials}, MaterialPages={Pages}", materialsBefore, pagesBefore);

            await DatabaseWipeService.WipeUserGeneratedContentAsync(db);

            var materialsAfter = await db.Materials.CountAsync();
            var pagesAfter = await db.MaterialPages.CountAsync();
            logger.LogWarning("After wipe: Materials={Materials}, MaterialPages={Pages}", materialsAfter, pagesAfter);
        }

        if ((wipeUserGenerated || wipeAllData) && noSeed)
        {
            logger.LogInformation("Wipe completed with --no-seed. Exiting by request.");
            return;
        }

        var adminPhone = app.Configuration["AdminSeed:PhoneNumber"];
        var adminPassword = app.Configuration["AdminSeed:Password"];
        var adminName = app.Configuration["AdminSeed:FullName"] ?? "System Admin";
        await AdminSeedService.EnsureAdminExistsAsync(db, sequenceService, adminPhone ?? string.Empty, adminPassword ?? string.Empty, adminName);

        await RoleTableBackfillService.BackfillAsync(db);

        if (wipeUserGenerated || wipeAllData)
        {
            logger.LogInformation("Wipe completed successfully. Exiting by request.");
            return;
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration/seed/backfill failed.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

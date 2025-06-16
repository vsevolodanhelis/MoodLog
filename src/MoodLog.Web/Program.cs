using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoodLog.Infrastructure.Data;
using MoodLog.Core.Interfaces;
using MoodLog.Infrastructure.Repositories;
using MoodLog.Application.Interfaces;
using MoodLog.Application.Services;
using MoodLog.Application.Services.AI;
using MoodLog.Application.Services.Gamification;
using MoodLog.Core.Events;
using MoodLog.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MoodLog.Web")));

// Add Identity services
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register repositories and services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IMoodEntryService, MoodEntryService>();
builder.Services.AddScoped<IMoodTagService, MoodTagService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<IMoodPredictionService, MoodPredictionService>();
builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<MockDataService>();

// Register new C# laboratory demonstration services
builder.Services.AddScoped<MoodLog.Application.Services.Diagnostics.ISystemDiagnosticsService,
    MoodLog.Application.Services.Diagnostics.SystemDiagnosticsService>();
builder.Services.AddHttpClient<MoodLog.Application.Services.External.IWellnessQuoteService,
    MoodLog.Application.Services.External.WellnessQuoteService>();
builder.Services.AddScoped<MoodLog.Application.Services.Background.DataCleanupService>();

// Register enhanced services for final touches
builder.Services.AddScoped<IMoodAnalyticsService, MoodAnalyticsService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddSingleton<MoodLog.Web.Services.PerformanceMonitoringService>();
builder.Services.AddSingleton<MoodLog.Web.Services.SecurityService>();

// Register event system with delegates
builder.Services.AddSingleton<IMoodEntryEventPublisher, MoodEntryEventPublisher>();
builder.Services.AddSingleton<MoodEntryAuditHandler>();
builder.Services.AddSingleton<MoodEntryNotificationHandler>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Add API support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MoodLog API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure event handlers using delegates
using (var scope = app.Services.CreateScope())
{
    var eventPublisher = scope.ServiceProvider.GetRequiredService<IMoodEntryEventPublisher>();
    var auditHandler = scope.ServiceProvider.GetRequiredService<MoodEntryAuditHandler>();
    var notificationHandler = scope.ServiceProvider.GetRequiredService<MoodEntryNotificationHandler>();

    // Subscribe to events using delegates
    eventPublisher.MoodEntryCreated += auditHandler.HandleMoodEntryCreated;
    eventPublisher.MoodEntryUpdated += auditHandler.HandleMoodEntryUpdated;
    eventPublisher.MoodEntryDeleted += auditHandler.HandleMoodEntryDeleted;
    eventPublisher.MoodEntryCreatedAsync += notificationHandler.HandleMoodEntryCreatedAsync;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Add enhanced error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Configure Swagger for development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoodLog API v1");
        c.RoutePrefix = "api/docs";
    });
}

app.MapStaticAssets();

app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Map API routes
app.MapControllers();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    context.Database.EnsureCreated();

    // Create Admin role if it doesn't exist
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Create default admin user if it doesn't exist
    var adminEmail = "admin@moodlog.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

app.Run();

using Microsoft.EntityFrameworkCore;
using NameParser.Application.Services;
using NameParser.Domain.Repositories;
using NameParser.Domain.Services;
using NameParser.Infrastructure.Data;
using NameParser.Infrastructure.Repositories;
using NameParser.Infrastructure.Services;
using NameParser.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add session support for file uploads
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Database Context
builder.Services.AddDbContext<RaceManagementContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("RaceManagementDb");
    options.UseSqlServer(connectionString);
});

// Configure Facebook Settings
builder.Services.Configure<FacebookSettings>(builder.Configuration.GetSection("Facebook"));

// Register HttpClient for FacebookService
builder.Services.AddHttpClient<FacebookService>();

// Register application services
builder.Services.AddScoped<IRaceResultRepository, ExcelRaceResultRepository>();
builder.Services.AddScoped<IRaceResultRepository, PdfRaceResultRepository>();
builder.Services.AddScoped<IMemberRepository, JsonMemberRepository>();
builder.Services.AddScoped<RaceRepository>();
builder.Services.AddScoped<ClassificationRepository>();
builder.Services.AddScoped<RaceProcessingService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PointsCalculationService>();
builder.Services.AddScoped<ReportGenerationService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<FileOutputService>();
builder.Services.AddScoped<FacebookService>();

// Configure localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { "en", "fr" };
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RaceManagementContext>();
    try
    {
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseRequestLocalization();

app.UseAuthorization();

app.MapRazorPages();

app.Run();

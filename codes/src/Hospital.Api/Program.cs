using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hospital.Api.Middleware;
using Hospital.Api.Services;
using Hospital.Application.Common.Interfaces;
using Hospital.Application.Common.Models;
using Hospital.Infrastructure;
using Hospital.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext().WriteTo.Console());
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false).AddEnvironmentVariables();

// Development gets a process-local key; restarting invalidates development sessions.
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:Secret"]))
    builder.Configuration["JwtSettings:Secret"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
var secret = builder.Configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("Cần cấu hình JwtSettings__Secret.");
if (Encoding.UTF8.GetByteCount(secret) < 32) throw new InvalidOperationException("Khóa JWT cần ít nhất 32 byte.");

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
if (builder.Configuration.GetValue("Notifications:WorkerEnabled", true)) builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateIssuer = true, ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true, ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal!;
            if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                || !int.TryParse(principal.FindFirstValue("tokenVersion"), out var version))
            { context.Fail("Phiên không hợp lệ."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<HospitalDbContext>();
            var user = await db.Users.AsNoTracking().Include(u => u.Role).SingleOrDefaultAsync(u => u.Id == id);
            if (user == null || !user.IsActive || user.TokenVersion != version || user.Role.Name != principal.FindFirstValue(ClaimTypes.Role))
                context.Fail("Phiên hết hiệu lực; vui lòng đăng nhập lại.");
        },
        OnChallenge = async context =>
        {
            context.HandleResponse(); context.Response.StatusCode = 401;
            context.Response.Headers.WWWAuthenticate = "Bearer";
            var body = ApiResponse.Failed("Cần đăng nhập hoặc phiên đã hết hiệu lực.", "UNAUTHORIZED");
            body.TraceId = context.HttpContext.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(body);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            var body = ApiResponse.Failed("Bạn không có quyền thực hiện thao tác này.", "FORBIDDEN");
            body.TraceId = context.HttpContext.TraceIdentifier;
            await context.Response.WriteAsJsonAsync(body);
        }
    };
});
builder.Services.AddAuthorization();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    var errors = context.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
    var body = ApiResponse.Failed("Dữ liệu không hợp lệ.", "VALIDATION_ERROR", errors);
    body.TraceId = context.HttpContext.TraceIdentifier;
    return new BadRequestObjectResult(body);
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true }));
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.Headers.RetryAfter = "60";
        var body = ApiResponse.Failed("Quá nhiều yêu cầu. Vui lòng thử lại sau.", "RATE_LIMITED");
        body.TraceId = context.HttpContext.TraceIdentifier;
        await context.HttpContext.Response.WriteAsJsonAsync(body, token);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Hospital Appointment API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5174", "https://localhost:7122"])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
if (builder.Configuration.GetValue("Database:Initialize", app.Environment.IsDevelopment()))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Database:SeedDemo", false))
        await DbSeeder.SeedAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "Hospital Appointment API", status = "Running" }));
app.MapGet("/health/ready", async (HospitalDbContext db) =>
{
    try { return await db.Database.CanConnectAsync() ? Results.Ok(new { status = "Ready" }) : Results.StatusCode(503); }
    catch { return Results.StatusCode(503); }
});
app.Run();

public partial class Program;

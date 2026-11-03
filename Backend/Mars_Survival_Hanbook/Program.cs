using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Mars_Survival_Hanbook.BackgroundJobs;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Integrations.Nasa;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Mars_Survival_Hanbook.Services;
using Mars_Survival_Hanbook.Services.Discovery;
using Mars_Survival_Hanbook.Services.Editorial;
using Mars_Survival_Hanbook.Services.Ingestion;
using Mars_Survival_Hanbook.Services.Sources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<MarsDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("MarsDatabase")
    ?? throw new InvalidOperationException("Connection string 'MarsDatabase' is required.")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentActor>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<SourceService>();
builder.Services.AddSingleton<RawStorage>();
builder.Services.AddScoped<ContentService>();
builder.Services.AddScoped<SnapshotService>();
builder.Services.AddScoped<ArticleService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<DiscoveryService>();
builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<IngestionProcessor>();
builder.Services.AddSingleton<NasaUrlPolicy>();
builder.Services.AddSingleton<WorkerHeartbeat>();
builder.Services.AddHttpClient<SafeNasaHttp>(c => c.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(sp => sp.GetRequiredService<NasaUrlPolicy>().CreateHandler());
builder.Services.AddScoped<NasaConnectorClient>();
builder.Services.AddHostedService<IngestionWorker>();
var key = builder.Configuration["Jwt:SigningKey"];
var commandMode = args.FirstOrDefault() is "seed" or "backfill" or "provision";
if (string.IsNullOrEmpty(key) && (builder.Environment.IsDevelopment() || commandMode))
    key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
if (key == null || Encoding.UTF8.GetByteCount(key) < 32)
    throw new InvalidOperationException("Configure Jwt:SigningKey with at least 32 bytes using user secrets or environment.");
var jwt = new JwtSettings(builder.Configuration["Jwt:Issuer"] ?? "MarsSurvivalHandbook",
    builder.Configuration["Jwt:Audience"] ?? "MarsSurvivalHandbook", key, builder.Configuration.GetValue("Jwt:ExpiryMinutes", 60));
builder.Services.AddSingleton(jwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new() {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateLifetime = true, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    o.Events = new JwtBearerEvents {
        OnTokenValidated = async context =>
        {
            if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) { context.Fail("Invalid account."); return; }
            var db = context.HttpContext.RequestServices.GetRequiredService<MarsDbContext>();
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, context.HttpContext.RequestAborted);
            if (user == null || user.State != UserState.Active || user.ArchivedAt != null ||
                context.Principal!.FindFirstValue(ClaimTypes.Role) != user.Role.ToString()) context.Fail("Account or role changed.");
        }
    };
});
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Cms", p => p.RequireRole("Editor", "ScientificReviewer", "Administrator"));
    o.AddPolicy("Edit", p => p.RequireRole("Editor", "Administrator"));
    o.AddPolicy("Review", p => p.RequireRole("ScientificReviewer"));
    o.AddPolicy("ReviewRead", p => p.RequireRole("ScientificReviewer", "Administrator"));
    o.AddPolicy("Admin", p => p.RequireRole("Administrator"));
});
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddControllers(o => o.ModelBinderProviders.Insert(0, new StrictEnumModelBinderProvider())).AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    o.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
});
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ctx =>
{
    var problem = new ValidationProblemDetails(ctx.ModelState) { Status = 400, Title = "Dữ liệu không hợp lệ." };
    problem.Extensions["code"] = "invalid_request"; problem.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
    return new BadRequestObjectResult(problem);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new() { Title = "Discover Mars API", Version = "v1",
        Description = "Plain-text editorial content. Public reads use immutable published snapshots. Admin mutations require If-Match." });
    o.AddSecurityDefinition("Bearer", new() { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    o.OperationFilter<DiscoveryOpenApiFilter>();
    o.SchemaFilter<DiscoverySchemaFilter>();
    o.SupportNonNullableReferenceTypes();
});
var app = builder.Build();
if (await DiscoveryCommands.RunAsync(args, app.Services)) return;
app.UseMiddleware<ApiExceptionMiddleware>();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/discover-mars"))
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/admin/discover-mars/health", async (MarsDbContext db, WorkerHeartbeat heartbeat, CancellationToken ct) =>
    Results.Ok(new { DatabaseReady = await db.Database.CanConnectAsync(ct), heartbeat.LastPoll })).RequireAuthorization("Admin");
app.Run();
public partial class Program { }

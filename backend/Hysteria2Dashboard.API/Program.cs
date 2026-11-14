using Hysteria2Dashboard.API.Hubs;
using Hysteria2Dashboard.API.Middleware;
using Hysteria2Dashboard.Application.Interfaces;
using Hysteria2Dashboard.Application.Services;
using Hysteria2Dashboard.Application.Services.Interfaces;
using Hysteria2Dashboard.Domain.Interfaces;
using Hysteria2Dashboard.Infrastructure.Repositories;
using Hysteria2Dashboard.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

IServiceProvider? rootProvider = null;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });

    options.AddSecurityDefinition("RouteSalt", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "X-Route-Salt",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "RouteSalt"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                var store = rootProvider!.GetRequiredService<IAppConfigStore>();
                var secret = store.GetJwtSecretAsync().GetAwaiter().GetResult();
                return [new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))];
            },
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<YamlConfigRepository>();
builder.Services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<YamlConfigRepository>());
builder.Services.AddSingleton<IHysteriaSettingsStore>(sp => sp.GetRequiredService<YamlConfigRepository>());
builder.Services.AddSingleton<JsonAppConfigStore>();
builder.Services.AddSingleton<IAppConfigStore>(sp => sp.GetRequiredService<JsonAppConfigStore>());
builder.Services.AddSingleton<IKeySettingsStore>(sp => sp.GetRequiredService<JsonAppConfigStore>());
builder.Services.AddSingleton<SshServerService>();
builder.Services.AddSingleton<IHysteriaService>(sp => sp.GetRequiredService<SshServerService>());
builder.Services.AddSingleton<IDashboardService>(sp => sp.GetRequiredService<SshServerService>());
builder.Services.AddSingleton<IUsageProvider>(sp => sp.GetRequiredService<SshServerService>());
builder.Services.AddSingleton<ITrafficRepository, SqliteTrafficRepository>();

builder.Services.AddHttpClient<ITrafficProvider, HysteriaTrafficClient>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<ITrafficService, TrafficService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IUsageService, UsageService>();

builder.Services.AddHostedService<TrafficBroadcastService>();
builder.Services.AddHostedService<StatusBroadcastService>();
builder.Services.AddHostedService<UsageBroadcastService>();

var app = builder.Build();
rootProvider = app.Services;

try
{
    var hysteriaConfig = app.Services.GetRequiredService<YamlConfigRepository>();

    if (await hysteriaConfig.GetLegacySniAsync() is { } legacySni)
        await app.Services.GetRequiredService<JsonAppConfigStore>().SaveSniAsync(legacySni);

    await hysteriaConfig.NormalizeAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Failed to migrate Hysteria2 config");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");

app.UseMiddleware<RouteSaltMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<TrafficHub>("/hubs/traffic");
app.MapHub<StatusHub>("/hubs/status");
app.MapHub<UsageHub>("/hubs/usage");

app.Run();

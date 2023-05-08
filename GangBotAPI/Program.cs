using Assets.Networking.DataBase.RedisProvider;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Debug.WriteLine("Authentication failed: " + context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Debug.WriteLine("OnChallenge: " + context.Error);
                    return Task.CompletedTask;
                }
            };
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DiscordAllowlist", policy =>
        policy.RequireAssertion(context =>
        {
            var discordUserIdClaim = context.User.FindFirst(claim => claim.Type == "DiscordUserId");
            if (discordUserIdClaim != null)
            {
                var discordUserId = discordUserIdClaim.Value;
                return IsUserInAllowlist(discordUserId, builder);
            }

            return false;
        }));
});

builder.Services.AddControllers();
builder.Services.AddSingleton<RedisProviderService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseCors(x => x
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .SetIsOriginAllowed(origin => true)
                    .AllowCredentials());



app.Run();


bool IsUserInAllowlist(string discordUserId, WebApplicationBuilder? buider)
{
    var allowlist = builder.Configuration.GetSection("DiscordAllowlist").Get<List<User>>();
    return allowlist?.Exists(user => user.Id == discordUserId) ?? false;
}

class User
{
    public string Id { get; set; }
    public string Name { get; set; }
}

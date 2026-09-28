using System.Text;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReviewsAssistant.Application.Ai.Services;
using ReviewsAssistant.Application.Reviews;
using ReviewsAssistant.Infrastructure.Ai;
using ReviewsAssistant.Infrastructure.Data;
using ReviewsAssistant.Infrastructure.Reviews;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Reviews")
    ?? throw new InvalidOperationException("Connection string 'Reviews' is required.");
var jwtSettings = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSettings["SigningKey"] ?? throw new InvalidOperationException("JWT signing key is required.");

builder.Services.AddDbContext<ReviewsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<IReviewService, ReviewService>();

var aiProvider = builder.Configuration["Ai:Provider"]
    ?? throw new InvalidOperationException("AI provider is required.");

if (string.Equals(aiProvider, "Stub", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IAiReviewAnalyzer, StubAiReviewAnalyzer>();
    builder.Services.AddScoped<IAiResponseGenerator, StubAiResponseGenerator>();
}
else if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase))
{
    var openAiOptions = builder.Configuration.GetSection(OpenAiOptions.SectionName).Get<OpenAiOptions>()
        ?? throw new InvalidOperationException("OpenAI settings are required.");
    if (string.IsNullOrWhiteSpace(openAiOptions.ApiKey) || string.IsNullOrWhiteSpace(openAiOptions.Model))
    {
        throw new InvalidOperationException("OpenAI API key and model are required.");
    }

    builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
    builder.Services.AddHttpClient<IAiReviewAnalyzer, OpenAiReviewAnalyzer>(client =>
    {
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiOptions.ApiKey);
    });
    builder.Services.AddHttpClient<IAiResponseGenerator, OpenAiResponseGenerator>(client =>
    {
        client.BaseAddress = new Uri("https://api.openai.com/v1/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openAiOptions.ApiKey);
    });
}
else
{
    throw new InvalidOperationException($"Unknown AI provider '{aiProvider}'.");
}

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins("http://localhost:4200", "https://localhost:4200").AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        ValidateLifetime = true
    };
});
builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ReviewsDbContext>().Database.MigrateAsync();
}
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;

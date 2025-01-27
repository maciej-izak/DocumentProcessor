using DocumentProcessorApi.Authentication;
using DocumentProcessorApi.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddScoped<IFileProcessor, FileProcessor>();

// Add services to the container.
builder.Services.AddControllers();

// Add Basic Authentication
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseRouting();

// Use Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 🔹 Add Health Check
app.MapHealthChecks("/health");

app.Run();
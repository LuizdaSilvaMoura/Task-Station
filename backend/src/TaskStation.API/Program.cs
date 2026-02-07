using System.Text.Json.Serialization;
using TaskStation.API.Middleware;
using TaskStation.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── JSON serialization ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Task Station API",
        Version = "v1",
        Description = "Task management system with SLA monitoring"
    });
});

// ── CORS (allow frontend dev server) ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ── Application + Infrastructure (Clean Architecture DI) ──
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ── Middleware pipeline ──
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Station API v1"));
}

app.UseCors("AllowFrontend");
app.MapControllers();

app.Run();

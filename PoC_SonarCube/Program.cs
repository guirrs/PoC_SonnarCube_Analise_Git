using Microsoft.Extensions.Options;
using PoC_SonarCube.Application.APIs;
using PoC_SonarCube.Application.Options;
using PoC_SonarCube.Application.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Configuracao API
builder.Services.Configure<SonarOptions>(builder.Configuration.GetSection("Sonar"));
builder.Services.AddHttpClient<SonarClientAPI>((sp, http) =>
{
    var o = sp.GetRequiredService<IOptions<SonarOptions>>().Value;
    http.BaseAddress = new Uri(o.Url.TrimEnd('/') + "/");
    var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.Token}:"));
    http.DefaultRequestHeaders.Authorization = new("Basic", basic);
});

//Services
builder.Services.AddSingleton<RunnerService>();
builder.Services.AddSingleton<ResultStoreService>();
builder.Services.AddScoped<GitServices>();
builder.Services.AddScoped<SonarScannerService>();
builder.Services.AddScoped<CommitAnalysisService>();


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

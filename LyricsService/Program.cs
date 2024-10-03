using System.Text.Json;
using Consul;
using Microsoft.Extensions.Options;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ILyricsService, LyricsService>();
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://localhost:8500");
}));

builder.Services.Configure<ServiceDiscoveryConfig>(builder.Configuration.GetSection("ServiceDiscoveryConfig"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConsul();

app.MapGet("/lyrics", async (HttpContext httpContext, ILyricsService lyricsService) =>
{
    var lyrics = lyricsService.GetLyrics();
    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, lyrics);
});

app.Run();

public interface ILyricsService
{
    List<Lyrics> GetLyrics();
}

public class LyricsService : ILyricsService
{
    private readonly List<Lyrics> _lyrics = new()
    {
        new Lyrics { SongId = 1, Text = "Bye Bye Bye ..." },
        new Lyrics { SongId = 2, Text = "You are my SPECIAL ..." },
        new Lyrics { SongId = 3, Text = "Welcome to the city of lies ..."},
        new Lyrics { SongId = 4, Text = "Society’s collapsing; hanging by a thread ..." },
        new Lyrics { SongId = 5, Text = "It's Britney, bitch ..." }
    };

    public List<Lyrics> GetLyrics()
    {
        return _lyrics;
    }
}

public record Lyrics
{
    public int SongId { get; init; }
    public string Text { get; init; }
}

public record ServiceDiscoveryConfig
{
    public string NameOfService { get; init; }
    public string IdOfService { get; init; }
    public string Host { get; init; }
    public int Port { get; init; }
}

public static class ConsulBuilderExtensions
{
    public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
    {

        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        var lifetime = app.ApplicationServices.GetRequiredService<IApplicationLifetime>();
        
        var settings = app.ApplicationServices.GetRequiredService<IOptions<ServiceDiscoveryConfig>>();

        var serviceName = settings.Value.NameOfService;
        var serviceId = settings.Value.IdOfService;
        var uri = new Uri($"http://{settings.Value.Host}:{settings.Value.Port}");

        var registration = new AgentServiceRegistration()
        {
            ID = serviceId,
            Name = serviceName,
            Address = $"{settings.Value.Host}",
            Port = uri.Port,
            Tags = new[] { $"urlprefix-/{settings.Value.IdOfService}" }
        };

        var result= consulClient.Agent.ServiceDeregister(registration.ID).Result;
        result = consulClient.Agent.ServiceRegister(registration).Result;

        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
        });

        return app;
    }
}
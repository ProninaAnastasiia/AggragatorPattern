using System.Text.Json;
using Consul;
using Microsoft.Extensions.Options;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMusicService, MusicService>();
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://localhost:8500");
}));

builder.Services.Configure<ServiceDiscoveryConfig>(builder.Configuration.GetSection("ServiceDiscoveryConfig"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseConsul();

app.MapGet("/songs", async (HttpContext httpContext, IMusicService musicService) =>
{
    var songs = musicService.GetSongs();
    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, songs);
});

app.Run();

public interface IMusicService
{
    List<Song> GetSongs();
}

public class MusicService : IMusicService
{
    private readonly List<Song> _songs = new()
    {
        new Song { Id = 1, Title = "Bye Bye Bye", Artist = "*NSYNC" },
        new Song { Id = 2, Title = "SPECIALZ", Artist = "King Gnu" },
        new Song { Id = 3, Title = "GOSSIP", Artist = "Måneskin" },
        new Song { Id = 4, Title = "Morality Lesson", Artist = "Will Stetson" },
        new Song { Id = 5, Title = "GIMME MORE", Artist = "Britney Spears" }
    };

    public List<Song> GetSongs()
    {
        return _songs;
    }
}

public record Song
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Artist { get; init; }
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
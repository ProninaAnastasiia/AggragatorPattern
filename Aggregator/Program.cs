using System.Text.Json;
using Consul;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IMusicService, MusicServiceClient>();
builder.Services.AddHttpClient<ILyricsService, LyricsServiceClient>();

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://localhost:8500");
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/music-lyrics", async (HttpContext httpContext, IMusicService musicService, ILyricsService lyricsService) =>
{
    var music = musicService.GetSongs();
    var lyrics = lyricsService.GetLyrics();

    var musicWithLyrics = music.Select(song => new
    {
        song.Id,
        song.Title,
        song.Artist,
        Lyrics = lyrics.Where(lyrics => lyrics.SongId == song.Id)
    });

    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, musicWithLyrics);
});

app.Run();

public interface IMusicService
{
    List<Song> GetSongs();
}

public interface ILyricsService
{
    List<Lyrics> GetLyrics();
}

public class MusicServiceClient : IMusicService
{
    private readonly HttpClient _httpClient;
    private readonly IConsulClient _consulClient;

    public MusicServiceClient(HttpClient httpClient, IConsulClient consulClient)
    {
        _httpClient = httpClient;
        _consulClient = consulClient;
    }

    public List<Song> GetSongs()
    {
        var services = _consulClient.Agent.Services().Result.Response;
        var musicService = services.Values.FirstOrDefault(s => s.Service.Equals("music"));
        var response = _httpClient.GetStringAsync($"http://{musicService.Address}:{musicService.Port}/songs").Result;
        var songs = JsonSerializer.Deserialize<List<Song>>(response);
        return songs;
    }
}

public class LyricsServiceClient : ILyricsService
{
    private readonly HttpClient _httpClient;
    private readonly IConsulClient _consulClient;
    
    public LyricsServiceClient(HttpClient httpClient, IConsulClient consulClient)
    {
        _httpClient = httpClient;
        _consulClient = consulClient;
    }

    public List<Lyrics> GetLyrics()
    {
        var services = _consulClient.Agent.Services().Result.Response;
        var lyricsService = services.Values.FirstOrDefault(s => s.Service.Equals("lyrics"));
        var response = _httpClient.GetStringAsync($"http://{lyricsService.Address}:{lyricsService.Port}/lyrics").Result;
        var lyrics = JsonSerializer.Deserialize<List<Lyrics>>(response);
        return lyrics;
    }
}

public record Song
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string Artist { get; init; }
}

public record Lyrics
{
    public int SongId { get; init; }
    public string Text { get; init; }
}
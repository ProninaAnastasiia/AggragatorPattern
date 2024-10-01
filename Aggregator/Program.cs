using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IMusicService, MusicServiceClient>();
builder.Services.AddHttpClient<ILyricsService, LyricsServiceClient>();

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

    public MusicServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Song> GetSongs()
    {
        var response = _httpClient.GetStringAsync($"http://localhost:5167/songs").Result;
        var songs = JsonSerializer.Deserialize<List<Song>>(response);
        return songs;
    }
}

public class LyricsServiceClient : ILyricsService
{
    private readonly HttpClient _httpClient;

    public LyricsServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public List<Lyrics> GetLyrics()
    {
        var response = _httpClient.GetStringAsync($"http://localhost:5259/lyrics").Result;
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
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ILyricsService, LyricsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
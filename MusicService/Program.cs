using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMusicService, MusicService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/songs", async (HttpContext httpContext, IMusicService musicService) =>
{
    var songs = musicService.GetSongs();
    httpContext.Response.ContentType = "application/json";
    await JsonSerializer.SerializeAsync(httpContext.Response.Body, songs);
});

//app.UseHttpsRedirection();

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
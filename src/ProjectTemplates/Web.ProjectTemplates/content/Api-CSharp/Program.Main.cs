using System.Text.Json.Serialization;
namespace Company.WebApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.AddContext<AppJsonSerializerContext>();
        });

        var app = builder.Build();

        var sampleAlbums = AlbumGenerator.GenerateAlbums().ToArray();

        var api = app.MapGroup("/albums");
        api.MapGet("/", () => sampleAlbums);
        api.MapGet("/{id}", (int id) =>
            sampleAlbums.FirstOrDefault(a => a.Id == id) is { } album
                ? Results.Ok(album)
                : Results.NotFound($"Album with id {id} not found"));

        app.Run();
    }
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Album))]
[JsonSerializable(typeof(Album[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

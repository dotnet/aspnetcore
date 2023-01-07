using System.Text.Json.Serialization;
using Company.WebApplication1;

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

[JsonSerializable(typeof(Album))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}

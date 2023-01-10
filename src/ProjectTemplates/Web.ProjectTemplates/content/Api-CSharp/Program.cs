using System.Text.Json.Serialization;
#if (NativeAot)
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
#endif
using Company.WebApplication1;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sampleAlbums = AlbumGenerator.GenerateAlbums().ToArray();

#if (NativeAot)
var jsonHttpOptions = app.Services.GetRequiredService<IOptions<JsonOptions>>().Value;
var jsonSerializerContext = new AppJsonSerializerContext(jsonHttpOptions.SerializerOptions);

app.MapGet("/albums", (HttpContext context) => context.Response.WriteAsJsonAsync(sampleAlbums, jsonSerializerContext.AlbumArray));
app.MapGet("/albums/{id}", (HttpContext context) =>
{
    if (!int.TryParse(context.GetRouteValue("id")?.ToString(), out int id))
    {
        context.Response.StatusCode = 400;
        return Task.CompletedTask;
    }

    if (sampleAlbums.FirstOrDefault(a => a.Id == id) is { } album)
    {
        return context.Response.WriteAsJsonAsync(album, jsonSerializerContext.Album);
    }

    context.Response.StatusCode = 404;
    return context.Response.WriteAsJsonAsync($"Album with id {id} not found", jsonSerializerContext.String);
});
#else
var api = app.MapGroup("/albums");
api.MapGet("/", () => sampleAlbums);
api.MapGet("/{id}", (int id) =>
    sampleAlbums.FirstOrDefault(a => a.Id == id) is { } album
        ? Results.Ok(album)
        : Results.NotFound($"Album with id {id} not found"));
#endif

app.Run();

#if (NativeAot)
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Album))]
[JsonSerializable(typeof(Album[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
#endif

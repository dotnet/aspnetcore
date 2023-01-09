namespace Company.WebApplication1;

public class Album
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public string? Artist { get; set; }

    public DateOnly FirstReleased { get; set; }

    public int TrackCount { get; set; }

    public double Price { get; set; }
}

static class AlbumGenerator
{
    private static readonly string[] _words =new[]
        {
            "Exceptional", "Forgotten", "Comfortable", "Dramatic", "Temporary", "Secret", "Memory", "Dancing", "Feeling", "Nature"
        };

    internal static IEnumerable<Album> GenerateAlbums(int count = 5)
    {
        var wordCombos = new List<int[]>();
        for (var i = 0; i < _words.Length; i++)
        {
            wordCombos.Add(new[] { i });

            for (var j = 0; j < _words.Length; j++)
            {
                if (i == j) continue;

                wordCombos.Add(new[] { i, j });
            }
        }

        var random = new Random();

        for (var id = 1; id <= count; id++)
        {
            yield return new Album
            {
                Id = id,
                Title = GetNextName(),
                Artist = GetNextName(),
                FirstReleased = DateOnly.FromDateTime(DateTime.Now.AddDays(random.Next(-365 * 60, -1))),
                TrackCount = random.Next(10, 30),
                Price = random.Next(7, 35) + 0.99
            };

            string GetNextName()
            {
                var index = random.Next(0, wordCombos.Count - 1);
                var combo = wordCombos[index];
                wordCombos.RemoveAt(index);
                return string.Join(' ', combo.Select(i => _words[i]));
            }
        }
    }
}

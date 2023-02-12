namespace Company.ApiApplication1;

public class Todo
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public DateOnly? DueBy { get; set; }

    public bool IsComplete { get; set; }
}

static class TodoGenerator
{
    private static readonly (string[] Prefixes, string[] Suffixes)[] _parts = new[]
        {
            (new[] { "Walk the", "Feed the" }, new[] { "dog", "cat", "goat" }),
            (new[] { "Do the", "Put away the" }, new[] { "groceries", "dishes", "laundry" }),
            (new[] { "Clean the" }, new[] { "bathroom", "pool", "blinds", "car" })
        };

    internal static IEnumerable<Todo> GenerateTodos(int count = 5)
    {
        var titleCount = _parts.Sum(row => row.Prefixes.Length * row.Suffixes.Length);
        var titleMap = new (int Row, int Prefix, int Suffix)[titleCount];
        var mapCount = 0;
        for (var i = 0; i < _parts.Length; i++)
        {
            var prefixes = _parts[i].Prefixes;
            var suffixes = _parts[i].Suffixes;
            for (var j = 0; j < prefixes.Length; j++)
            {
                for (var k = 0; k < suffixes.Length; k++)
                {
                    titleMap[mapCount++] = (i, j, k);
                }
            }
        }

        Random.Shared.Shuffle(titleMap);

        for (var id = 1; id <= count; id++)
        {
            var (rowIndex, prefixIndex, suffixIndex) = titleMap[id];
            var (prefixes, suffixes) = _parts[rowIndex];
            yield return new Todo
            {
                Id = id,
                Title = string.Join(' ', prefixes[prefixIndex], suffixes[suffixIndex]),
                DueBy = Random.Shared.Next(-200, 365) switch
                {
                    < 0 => null,
                    var days => DateOnly.FromDateTime(DateTime.Now.AddDays(days))
                }
            };
        }
    }
}

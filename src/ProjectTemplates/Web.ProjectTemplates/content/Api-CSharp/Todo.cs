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
        var titleMap = new List<(int Row, int Prefix, int Suffix)>();
        for (var i = 0; i < _parts.Length; i++)
        {
            var prefixes = _parts[i].Prefixes;
            var suffixes = _parts[i].Suffixes;
            for (int j = 0; j < prefixes.Length; j++)
            {
                for (int k = 0; k < suffixes.Length; k++)
                {
                    titleMap.Add((i, j, k));
                }
            }
        }

        var random = new Random();

        for (var id = 1; id <= count; id++)
        {
            yield return new Todo
            {
                Id = id,
                Title = GetNextTitle(),
                DueBy = random.Next(-200, 365) switch
                {
                    < 0 => null,
                    var days => DateOnly.FromDateTime(DateTime.Now.AddDays(days))
                }
            };

            string GetNextTitle()
            {
                var index = random.Next(0, titleMap.Count - 1);
                var map = titleMap[index];
                var row = _parts[map.Row];
                titleMap.RemoveAt(index);
                return string.Join(' ', row.Prefixes[map.Prefix], row.Suffixes[map.Suffix]);
            }
        }
    }
}

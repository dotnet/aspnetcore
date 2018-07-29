using System.Collections.Generic;

namespace Microsoft.Repl.Commanding
{
    public class CommandOptionSpecification
    {
        public string Id { get; }

        public IReadOnlyList<string> Forms { get; }

        public int MaximumOccurrences { get; }

        public int MinimumOccurrences { get; }

        public bool AcceptsValue { get; }

        public bool RequiresValue { get; }

        public CommandOptionSpecification(string id, bool acceptsValue = false, bool requiresValue = false, int minimumOccurrences = 0, int maximumOccurrences = int.MaxValue, params string[] forms)
        {
            Id = id;
            Forms = forms;
            MinimumOccurrences = minimumOccurrences;
            MaximumOccurrences = maximumOccurrences > minimumOccurrences ? maximumOccurrences : minimumOccurrences;
            RequiresValue = requiresValue;
            AcceptsValue = RequiresValue || acceptsValue;
        }
    }
}
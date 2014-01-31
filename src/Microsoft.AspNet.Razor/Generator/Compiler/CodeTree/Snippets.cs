using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class Snippets : List<Snippet>
    {
        public Snippets() {}

        public Snippets(int capacity)
            : base(capacity) {}

        public Snippets(IEnumerable<Snippet> collection)
            : base(collection) {}

        public Snippets(Snippets collection)
            : base(collection) {}

        public Snippets(string value)
            : base(new[] { new Snippet { Value = value } }) {}

        public override string ToString()
        {
            return string.Concat(this.Select(s => s.Value).ToArray());
        }
    }
}

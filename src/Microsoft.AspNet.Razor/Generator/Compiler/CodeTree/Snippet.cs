using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class Snippet
    {
        public Snippet() {}

        public Snippet(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
        public SourceSpan View { get; set; }
    }
}

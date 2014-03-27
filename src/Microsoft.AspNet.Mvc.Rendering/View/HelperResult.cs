using System;
using System.IO;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HelperResult
    {
        private readonly Action<TextWriter> _action;

        public HelperResult([NotNull] Action<TextWriter> action)
        {
            _action = action;
        }

        public void WriteTo([NotNull] TextWriter writer)
        {
            _action(writer);
        }
    }
}

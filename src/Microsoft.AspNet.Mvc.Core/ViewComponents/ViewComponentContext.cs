
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentContext
    {
        public ViewComponentContext([NotNull] TypeInfo componentType, [NotNull] ViewContext viewContext,
            [NotNull] TextWriter writer)
        {
            ComponentType = componentType;
            ViewContext = viewContext;
            Writer = writer;
        }

        public TypeInfo ComponentType { get; private set; }

        public ViewContext ViewContext { get; private set; }

        public TextWriter Writer { get; private set; }
    }
}

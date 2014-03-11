using System;

namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptor
    {
        public FilterDescriptor([NotNull]IFilter filter, int filterScope)
        {
            Filter = filter;
            Scope = filterScope;
        }

        public IFilter Filter { get; private set; }
        public int Scope { get; private set; }
    }
}

using System;

namespace Microsoft.AspNet.Mvc
{
    public class FilterDescriptor
    {
        public FilterDescriptor([NotNull]IFilter filter, int origin)
        {
            Filter = filter;
            Origin = origin;
        }

        public IFilter Filter { get; private set; }
        public int Origin { get; private set; }
    }
}

using System;

namespace Microsoft.AspNet.Mvc
{
    public interface ITypeFilter : IFilter
    {
        object[] Arguments { get; }

        Type ImplementationType { get; }
    }
}

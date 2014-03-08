using System;

namespace Microsoft.AspNet.Mvc
{
    public interface ITypeFilter : IFilter
    {
        Type ImplementationType { get; }
    }
}

using System;

namespace Microsoft.AspNet.Mvc
{
    public interface IServiceFilter : IFilter
    {
        Type ServiceType { get; }
    }
}

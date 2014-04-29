using System;

namespace Microsoft.AspNet.Mvc
{
    public interface IFilterFactory : IFilter
    {
        IFilter CreateInstance([NotNull] IServiceProvider serviceProvider);
    }
}

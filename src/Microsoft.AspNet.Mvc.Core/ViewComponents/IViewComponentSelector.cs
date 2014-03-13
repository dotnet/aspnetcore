
using System;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentSelector
    {
        Type SelectComponent([NotNull] string componentName);
    }
}

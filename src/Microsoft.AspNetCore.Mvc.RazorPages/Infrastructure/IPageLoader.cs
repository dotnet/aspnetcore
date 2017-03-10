using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageLoader
    {
        CompiledPageActionDescriptor Load(PageActionDescriptor actionDescriptor);
    }
}

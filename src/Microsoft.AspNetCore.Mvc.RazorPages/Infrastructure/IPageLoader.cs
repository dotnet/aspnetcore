using System;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public interface IPageLoader
    {
        Type Load(PageActionDescriptor actionDescriptor);
    }
}

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public interface IFilter<T>
    {
        Task Invoke(T context, Func<T, Task> next);
    }
}

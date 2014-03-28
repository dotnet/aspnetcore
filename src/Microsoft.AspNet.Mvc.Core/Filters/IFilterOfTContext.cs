using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public interface IFilter<TContext>
    {
        Task Invoke(TContext context, Func<Task> next);
    }
}

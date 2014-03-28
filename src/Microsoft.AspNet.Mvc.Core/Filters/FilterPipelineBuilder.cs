using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class FilterPipelineBuilder<TContext>
    {
        private readonly IFilter<TContext>[] _filters;
        private readonly TContext _context;

        // FilterDescriptors are already ordered externally.
        public FilterPipelineBuilder(IEnumerable<IFilter<TContext>> filters, TContext context)
        {
            _filters = filters.ToArray();
            _context = context;
        }

        public async Task InvokeAsync()
        {
            var caller = new CallNextAsync(_context, _filters);
            
            await caller.CallNextProvider();
        }

        private class CallNextAsync
        {
            private readonly TContext _context;
            private readonly IFilter<TContext>[] _filters;
            private readonly Func<Task> _next;

            private int _index;

            public CallNextAsync(TContext context, IFilter<TContext>[] filters)
            {
                _context = context;
                _next = CallNextProvider;
                _filters = filters;
            }

            public async Task CallNextProvider()
            {
                if (_filters.Length > _index)
                {
                    await _filters[_index++].Invoke(_context, _next);
                }
            }
        }
    }
}

using System;
using System.Threading;
using Microsoft.AspNet.Mvc;

namespace BasicWebSite
{
    public class ActionDescriptorCreationCounter : IActionDescriptorProvider
    {
        private long _callCount;

        public long CallCount
        {
            get
            {
                var callCount = Interlocked.Read(ref _callCount);

                return callCount;
            }
        }

        public int Order
        {
            get
            {
                return DefaultOrder.DefaultFrameworkSortOrder - 100;
            }
        }

        public void Invoke(ActionDescriptorProviderContext context, Action callNext)
        {
            callNext();

            if (context.Results.Count == 0)
            {
                throw new InvalidOperationException("No actions found!");
            }

            Interlocked.Increment(ref _callCount);
        }
    }
}
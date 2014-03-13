
using System;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentInvokerProvider : IViewComponentInvokerProvider
    {
        private readonly IServiceProvider _serviceProvider;

        public DefaultViewComponentInvokerProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public int Order
        {
            get { return 0; }
        }

        public void Invoke([NotNull] ViewComponentInvokerProviderContext context, [NotNull] Action callNext)
        {
            context.Result = new DefaultViewComponentInvoker(_serviceProvider, context.ComponentType, context.Arguments);
            callNext();
        }
    }
}

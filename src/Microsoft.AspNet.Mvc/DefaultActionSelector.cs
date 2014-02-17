using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionSelector : IActionSelector
    {
        private readonly IEnumerable<IActionDescriptorProvider> _actionDescriptorProviders;

        public DefaultActionSelector(IEnumerable<IActionDescriptorProvider> actionDescriptorProviders)
        {
            _actionDescriptorProviders = actionDescriptorProviders;
        }

        public ActionDescriptor Select(RequestContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            var allDescriptors = _actionDescriptorProviders.SelectMany(d => d.GetDescriptors());

            return allDescriptors.SingleOrDefault(d => Match(d, context));
        }

        public bool Match(ActionDescriptor descriptor, RequestContext context)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException("descriptor");
            }

            return (descriptor.RouteConstraints == null || descriptor.RouteConstraints.All(c => c.Accept(context))) &&
                   (descriptor.MethodConstraints == null || descriptor.MethodConstraints.All(c => c.Accept(context))) &&
                   (descriptor.DynamicConstraints == null || descriptor.DynamicConstraints.All(c => c.Accept(context)));
        }
    }
}

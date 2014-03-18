using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptor
    {
        public virtual string Name { get; set; }

        public List<RouteDataActionConstraint> RouteConstraints { get; set; }

        public List<HttpMethodConstraint> MethodConstraints { get; set; }

        public List<IActionConstraint> DynamicConstraints { get; set; }

        public List<ParameterDescriptor> Parameters { get; set; }

        public List<FilterDescriptor> FilterDescriptors { get; set; }
    }
}

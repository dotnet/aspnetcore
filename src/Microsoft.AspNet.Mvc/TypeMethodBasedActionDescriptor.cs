using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    [DebuggerDisplay("CA {Path}:{Name}(RC-{RouteConstraints.Count})")]
    public class TypeMethodBasedActionDescriptor : ActionDescriptor
    {
        public override string Path
        {
            get
            {
                return ControllerDescriptor.Name;
            }
            set
            {
                throw new InvalidOperationException("Cannot override path");
            }
        }

        public string ControllerName
        {
            get
            {
                return ControllerDescriptor.Name;
            }
        }

        public MethodInfo MethodInfo { get; set; }

        public ControllerDescriptor ControllerDescriptor { get; set; }
    }
}

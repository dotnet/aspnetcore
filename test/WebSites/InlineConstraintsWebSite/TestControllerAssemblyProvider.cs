using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Mvc;

namespace InlineConstraints
{
    public class TestControllerAssemblyProvider : IControllerAssemblyProvider
    {
        public IEnumerable<Assembly> CandidateAssemblies
        {
            get
            {
                return new[] { typeof(TestControllerAssemblyProvider).GetTypeInfo().Assembly };
            }
        }
    }
}

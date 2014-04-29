using System;
using System.Diagnostics;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("TypeFilter: Type={ImplementationType} Order={Order}")]
    public class TypeFilterAttribute : Attribute, IFilterFactory, IOrderedFilter
    {
        public TypeFilterAttribute([NotNull] Type type)
        {
            ImplementationType = type;
        }

        public object[] Arguments { get; set; }

        public Type ImplementationType { get; private set; }

        public int Order { get; set; }

        public IFilter CreateInstance([NotNull] IServiceProvider serviceProvider)
        {
            var activator = serviceProvider.GetService<ITypeActivator>();
            var obj = activator.CreateInstance(serviceProvider, ImplementationType, Arguments ?? new object[0]);

            var filter = obj as IFilter;
            if (filter == null)
            {
                throw new InvalidOperationException(Resources.FormatFilterFactoryAttribute_TypeMustImplementIFilter(
                    typeof(TypeFilterAttribute).Name,
                    typeof(IFilter).Name));
            }

            return filter;
        }
    }
}
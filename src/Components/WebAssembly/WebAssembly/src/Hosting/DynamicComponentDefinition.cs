using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicComponentDefinition
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="componentType"></param>
        public DynamicComponentDefinition(string name, Type componentType)
        {
            Name = name;
            ComponentType = componentType;
            Parameters = new List<DynamicComponentParameter>();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public Type ComponentType { get; }

        internal bool HasCatchAllProperty { get; init; }

        internal bool HasCallbackParameter { get; init; }

        /// <summary>
        /// 
        /// </summary>
        public IList<DynamicComponentParameter> Parameters { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TParameter"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicComponentDefinition AddParameter<TParameter>(string name)
        {
            for (var i = 0; i < Parameters.Count; i++)
            {
                var parameter = Parameters[i];
                if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Parameter {name} already defined.");
                }
            }

            Parameters.Add(new DynamicComponentParameter(name, typeof(TParameter)));

            return this;
        }

        internal DynamicComponentParameter? GetParameter(string name)
        {
            foreach (var parameter in Parameters)
            {
                if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
                {
                    return parameter;
                }
            }

            return null;
        }
    }
}

using System;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class DynamicComponentParameter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameterType"></param>
        public DynamicComponentParameter(string name, Type parameterType)
        {
            Name = name;
            ParameterType = parameterType;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public Type ParameterType { get; }

        internal bool IsCallback { get; init; }
    }
}

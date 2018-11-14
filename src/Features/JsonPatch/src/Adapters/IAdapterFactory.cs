using Microsoft.AspNetCore.JsonPatch.Internal;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.JsonPatch.Adapters
{
    /// <summary>
    /// Defines the operations used for loading an <see cref="IAdapter"/> based on the current object and ContractResolver.
    /// </summary>
    public interface IAdapterFactory
    {
        /// <summary>
        /// Creates an <see cref="IAdapter"/> for the current object 
        /// </summary>
        /// <param name="target">The target object</param>
        /// <param name="contractResolver">The current contract resolver</param>
        /// <returns>The needed <see cref="IAdapter"/></returns>
        IAdapter Create(object target, IContractResolver contractResolver);
    }
}

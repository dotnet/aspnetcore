// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web.JSComponents
{
    /// <summary>
    /// Specifies options for use when enabling JS component support.
    /// This type is not normally used directly from application code. In most cases, applications should
    /// call methods on the <see cref="IJSComponentConfiguration" /> on their application host builder.
    /// </summary>
    public class JSComponentConfiguration
    {
        internal Dictionary<string, Type> JsComponentTypesByIdentifier { get; } = new (StringComparer.Ordinal);

        /// <summary>
        /// Registers the specified component type as being available for instantiation from JavaScript.
        /// </summary>
        /// <param name="identifier">A unique identifier.</param>
        /// <param name="componentType">The type of the component.</param>
        public void Add(string identifier, Type componentType)
            => JsComponentTypesByIdentifier.Add(identifier, componentType);
    }
}

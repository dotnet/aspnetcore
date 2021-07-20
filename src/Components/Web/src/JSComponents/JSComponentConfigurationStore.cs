// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Specifies options for use when enabling JS component support.
    /// This type is not normally used directly from application code. In most cases, applications should
    /// call methods on the <see cref="IJSComponentConfiguration" /> on their application host builder.
    /// </summary>
    public class JSComponentConfigurationStore
    {
        // Everything's internal here, and can only be operated upon via the extension methods on
        // IJSComponentConfiguration. This is so that, in the future, we can add any additional
        // configuration APIs (as further extension methods) and/or storage (as internal members here)
        // without needing any changes on the downstream code that implements IJSComponentConfiguration,
        // and without exposing any of the configuration storage across layers.

        internal Dictionary<string, Type> JsComponentTypesByIdentifier { get; } = new (StringComparer.Ordinal);

        /// <summary>
        /// Registers the specified component type as being available for instantiation from JavaScript.
        /// </summary>
        /// <param name="identifier">A unique identifier.</param>
        /// <param name="componentType">The type of the component.</param>
        internal void Add(string identifier, Type componentType)
            => JsComponentTypesByIdentifier.Add(identifier, componentType);
    }
}

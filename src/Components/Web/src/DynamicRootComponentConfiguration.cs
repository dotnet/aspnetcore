// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    /// <summary>
    /// Configures options for allowing JavaScript to add root components dynamically.
    /// </summary>
    public class DynamicRootComponentConfiguration
    {
        internal Dictionary<string, Type> AllowedComponentsByIdentifier { get; } = new(StringComparer.Ordinal);

        internal JsonSerializerOptions JsonOptions { get; }

        /// <summary>
        /// Constructs an instance of <see cref="DynamicRootComponentConfiguration" />
        /// </summary>
        /// <param name="jsonOptions"></param>
        public DynamicRootComponentConfiguration(JsonSerializerOptions jsonOptions)
        {
            JsonOptions = jsonOptions;
        }

        /// <summary>
        /// Marks the specified component type as allowed for instantiation from JavaScript.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DynamicRootComponentInterop))]
        public void Register<[DynamicallyAccessedMembers(Component)] TComponent>(string name)
        {
            AllowedComponentsByIdentifier.Add(name, typeof(TComponent));
        }
    }
}

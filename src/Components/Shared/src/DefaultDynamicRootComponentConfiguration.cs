// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Infrastructure;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.Web
{
    internal class DefaultDynamicRootComponentConfiguration : DynamicRootComponentConfiguration
    {
        public Dictionary<string, Type> AllowedComponentsByIdentifier { get; } = new(StringComparer.Ordinal);

        public JsonSerializerOptions JsonOptions { get; }

        public DefaultDynamicRootComponentConfiguration(JsonSerializerOptions jsonOptions)
        {
            JsonOptions = jsonOptions;
        }

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DynamicRootComponentInterop))]
        public override void Register<[DynamicallyAccessedMembers(Component)] TComponent>(string name)
        {
            AllowedComponentsByIdentifier.Add(name, typeof(TComponent));
        }
    }
}

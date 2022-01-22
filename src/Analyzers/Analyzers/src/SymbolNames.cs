// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Analyzers
{
    internal static class SymbolNames
    {
        public const string ConfigureServicesMethodPrefix = "Configure";

        public const string ConfigureServicesMethodSuffix = "Services";

        public const string ConfigureMethodPrefix = "Configure";

        public static class IApplicationBuilder
        {
            public const string MetadataName = "Microsoft.AspNetCore.Builder.IApplicationBuilder";
        }

        public static class IServiceCollection
        {
            public const string MetadataName = "Microsoft.Extensions.DependencyInjection.IServiceCollection";
        }

        public static class ComponentEndpointRouteBuilderExtensions
        {
            public const string MetadataName = "Microsoft.AspNetCore.Builder.ComponentEndpointRouteBuilderExtensions";

            public const string MapBlazorHubMethodName = "MapBlazorHub";
        }

        public static class HubEndpointRouteBuilderExtensions
        {
            public const string MetadataName = "Microsoft.AspNetCore.Builder.HubEndpointRouteBuilderExtensions";

            public const string MapHubMethodName = "MapHub";
        }

        public static class SignalRAppBuilderExtensions
        {
            public const string MetadataName = "Microsoft.AspNetCore.Builder.SignalRAppBuilderExtensions";

            public const string UseSignalRMethodName = "UseSignalR";
        }

        public static class MvcOptions
        {
            public const string MetadataName = "Microsoft.AspNetCore.Mvc.MvcOptions";

            public const string EnableEndpointRoutingPropertyName = "EnableEndpointRouting";
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Hosting.WebHostBuilderFactory
{
    internal class FactoryResolutionResult<TWebHost,TWebHostBuilder>
    {
        public FactoryResolutionResultKind ResultKind { get; set; }
        public Type ProgramType { get; set; }
        public Func<string[], TWebHost> WebHostFactory { get; set; }
        public Func<string[], TWebHostBuilder> WebHostBuilderFactory { get; set; }

        internal static FactoryResolutionResult<TWebHost, TWebHostBuilder> NoBuildWebHost(Type programType) =>
            new FactoryResolutionResult<TWebHost, TWebHostBuilder>
            {
                ProgramType = programType,
                ResultKind = FactoryResolutionResultKind.NoBuildWebHost
            };

        internal static FactoryResolutionResult<TWebHost, TWebHostBuilder> NoCreateWebHostBuilder(Type programType) =>
            new FactoryResolutionResult<TWebHost, TWebHostBuilder>
            {
                ProgramType = programType,
                ResultKind = FactoryResolutionResultKind.NoCreateWebHostBuilder
            };

        internal static FactoryResolutionResult<TWebHost, TWebHostBuilder> NoEntryPoint() =>
            new FactoryResolutionResult<TWebHost, TWebHostBuilder>
            {
                ResultKind = FactoryResolutionResultKind.NoEntryPoint
            };

        internal static FactoryResolutionResult<TWebHost, TWebHostBuilder> Succeded(Func<string[], TWebHost> factory, Type programType) => new FactoryResolutionResult<TWebHost, TWebHostBuilder>
        {
            ProgramType = programType,
            ResultKind = FactoryResolutionResultKind.Success,
            WebHostFactory = factory
        };

        internal static FactoryResolutionResult<TWebHost, TWebHostBuilder> Succeded(Func<string[], TWebHostBuilder> factory, Type programType) => new FactoryResolutionResult<TWebHost, TWebHostBuilder>
        {
            ProgramType = programType,
            ResultKind = FactoryResolutionResultKind.Success,
            WebHostBuilderFactory = factory,
            WebHostFactory = args =>
            {
                var builder = factory(args);
                return (TWebHost)builder.GetType().GetMethod("Build").Invoke(builder, Array.Empty<object>());
            }
        };
    }
}

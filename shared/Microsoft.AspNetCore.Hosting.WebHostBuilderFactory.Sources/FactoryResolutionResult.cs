// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Hosting.WebHostBuilderFactory
{
    internal class FactoryResolutionResult
    {
        public FactoryResolutionResultKind ResultKind { get; set; }
        public Type ProgramType { get; set; }
        public Func<string[], IWebHost> WebHostFactory { get; set; }
        public Func<string[], IWebHostBuilder> WebHostBuilderFactory { get; set; }

        internal static FactoryResolutionResult NoBuildWebHost(Type programType) =>
            new FactoryResolutionResult
            {
                ProgramType = programType,
                ResultKind = FactoryResolutionResultKind.NoBuildWebHost
            };

        internal static FactoryResolutionResult NoCreateWebHostBuilder(Type programType) =>
            new FactoryResolutionResult
            {
                ProgramType = programType,
                ResultKind = FactoryResolutionResultKind.NoCreateWebHostBuilder
            };

        internal static FactoryResolutionResult NoEntryPoint() =>
            new FactoryResolutionResult
            {
                ResultKind = FactoryResolutionResultKind.NoEntryPoint
            };

        internal static FactoryResolutionResult Succeded(Func<string[], IWebHost> factory, Type programType) => new FactoryResolutionResult
        {
            ProgramType = programType,
            ResultKind = FactoryResolutionResultKind.Success,
            WebHostFactory = factory
        };

        internal static FactoryResolutionResult Succeded(Func<string[], IWebHostBuilder> factory, Type programType) => new FactoryResolutionResult
        {
            ProgramType = programType,
            ResultKind = FactoryResolutionResultKind.Success,
            WebHostBuilderFactory = factory,
            WebHostFactory = args => factory(args).Build()
        };
    }
}

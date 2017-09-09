// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Hosting.WebHostBuilderFactory
{
    internal class WebHostFactoryResolver
    {
        public static readonly string CreateWebHostBuilder = nameof(CreateWebHostBuilder);
        public static readonly string BuildWebHost = nameof(BuildWebHost);

        public static FactoryResolutionResult ResolveWebHostBuilderFactory(Assembly assembly)
        {
            var programType = assembly?.EntryPoint?.DeclaringType;
            if (programType == null)
            {
                return FactoryResolutionResult.NoEntryPoint();
            }

            var factory = programType?.GetTypeInfo().GetDeclaredMethod(CreateWebHostBuilder);
            if (factory == null)
            {
                return FactoryResolutionResult.NoCreateWebHostBuilder(programType);
            }

            return FactoryResolutionResult.Succeded(args => (IWebHostBuilder)factory.Invoke(null, new object[] { args }), programType);
        }

        public static FactoryResolutionResult ResolveWebHostFactory(Assembly assembly)
        {
            // We want to give priority to BuildWebHost over CreateWebHostBuilder for backwards
            // compatibility with existing projects that follow the old pattern.
            var findResult = ResolveWebHostBuilderFactory(assembly);
            switch (findResult.ResultKind)
            {
                case FactoryResolutionResultKind.NoEntryPoint:
                    return findResult;
                case FactoryResolutionResultKind.Success:
                case FactoryResolutionResultKind.NoCreateWebHostBuilder:
                    var buildWebHostMethod = findResult.ProgramType.GetTypeInfo().GetDeclaredMethod("BuildWebHost");
                    if (buildWebHostMethod == null)
                    {
                        if (findResult.ResultKind == FactoryResolutionResultKind.Success)
                        {
                            return findResult;
                        }

                        return FactoryResolutionResult.NoBuildWebHost(findResult.ProgramType);
                    }
                    else
                    {
                        return FactoryResolutionResult.Succeded(args => (IWebHost)buildWebHostMethod.Invoke(null, new object[] { args }), findResult.ProgramType);
                    }
                case FactoryResolutionResultKind.NoBuildWebHost:
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}

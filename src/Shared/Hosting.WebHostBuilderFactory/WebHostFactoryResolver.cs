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

        public static FactoryResolutionResult<TWebhost,TWebhostBuilder> ResolveWebHostBuilderFactory<TWebhost, TWebhostBuilder>(Assembly assembly)
        {
            var programType = assembly?.EntryPoint?.DeclaringType;
            if (programType == null)
            {
                return FactoryResolutionResult<TWebhost, TWebhostBuilder>.NoEntryPoint();
            }

            var factory = programType.GetTypeInfo().GetDeclaredMethod(CreateWebHostBuilder);
            if (factory == null || 
                !typeof(TWebhostBuilder).IsAssignableFrom(factory.ReturnType) ||
                factory.GetParameters().Length != 1 ||
                !typeof(string []).Equals(factory.GetParameters()[0].ParameterType))
            {
                return FactoryResolutionResult<TWebhost, TWebhostBuilder>.NoCreateWebHostBuilder(programType);
            }

            return FactoryResolutionResult<TWebhost, TWebhostBuilder>.Succeded(args => (TWebhostBuilder)factory.Invoke(null, new object[] { args }), programType);
        }

        public static FactoryResolutionResult<TWebhost, TWebhostBuilder> ResolveWebHostFactory<TWebhost, TWebhostBuilder>(Assembly assembly)
        {
            // We want to give priority to BuildWebHost over CreateWebHostBuilder for backwards
            // compatibility with existing projects that follow the old pattern.
            var findResult = ResolveWebHostBuilderFactory<TWebhost, TWebhostBuilder>(assembly);
            switch (findResult.ResultKind)
            {
                case FactoryResolutionResultKind.NoEntryPoint:
                    return findResult;
                case FactoryResolutionResultKind.Success:
                case FactoryResolutionResultKind.NoCreateWebHostBuilder:
                    var buildWebHostMethod = findResult.ProgramType.GetTypeInfo().GetDeclaredMethod(BuildWebHost);
                    if (buildWebHostMethod == null ||
                        !typeof(TWebhost).IsAssignableFrom(buildWebHostMethod.ReturnType) ||
                        buildWebHostMethod.GetParameters().Length != 1 ||
                        !typeof(string[]).Equals(buildWebHostMethod.GetParameters()[0].ParameterType))
                    {
                        if (findResult.ResultKind == FactoryResolutionResultKind.Success)
                        {
                            return findResult;
                        }

                        return FactoryResolutionResult<TWebhost, TWebhostBuilder>.NoBuildWebHost(findResult.ProgramType);
                    }
                    else
                    {
                        return FactoryResolutionResult<TWebhost, TWebhostBuilder>.Succeded(args => (TWebhost)buildWebHostMethod.Invoke(null, new object[] { args }), findResult.ProgramType);
                    }
                case FactoryResolutionResultKind.NoBuildWebHost:
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}

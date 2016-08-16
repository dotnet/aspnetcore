// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal
{
    public class PrecompilationApplication : CommandLineApplication
    {
        private readonly Type _callingType;

        public PrecompilationApplication(Type callingType)
        {
            _callingType = callingType;

            Name = "razor-precompile";
            FullName = "Microsoft Razor Precompilation Utility";
            Description = "Precompiles Razor views.";
            ShortVersionGetter = GetInformationalVersion;

            HelpOption("-?|-h|--help");

            OnExecute(() =>
            {
                ShowHelp();
                return 2;
            });
        }

        public new int Execute(params string[] args)
        {
            try
            {
                return base.Execute(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
#if DEBUG
                Console.Error.WriteLine(ex);
#endif
                return 1;
            }
        }

        private string GetInformationalVersion()
        {
            var assembly = _callingType.GetTypeInfo().Assembly;
            var attributes = assembly.GetCustomAttributes(
                typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute[];

            var versionAttribute = attributes.Length == 0 ?
                assembly.GetName().Version.ToString() :
                attributes[0].InformationalVersion;

            return versionAttribute;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Internal
{
    internal class PrecompilationApplication : CommandLineApplication
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
                return base.Execute(ExpandResponseFiles(args));
            }
            catch (AggregateException ex) when (ex.InnerException != null)
            {
                Error.WriteLine(ex.InnerException.Message);
                Error.WriteLine(ex.InnerException.StackTrace);
                return 1;
            }
            catch (Exception ex)
            {
                Error.WriteLine(ex.Message);
                Error.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        private string GetInformationalVersion()
        {
            var assembly = _callingType.GetTypeInfo().Assembly;
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return attribute.InformationalVersion;
        }

        private static string[] ExpandResponseFiles(string[] args)
        {
            var expandedArgs = new List<string>();
            foreach (var arg in args)
            {
                if (!arg.StartsWith("@", StringComparison.Ordinal))
                {
                    expandedArgs.Add(arg);
                }
                else
                {
                    var fileName = arg.Substring(1);
                    expandedArgs.AddRange(File.ReadLines(fileName));    
                }
            }

            return expandedArgs.ToArray();
        }
    }
}

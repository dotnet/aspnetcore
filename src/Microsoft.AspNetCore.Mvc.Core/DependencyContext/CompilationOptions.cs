// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    internal class CompilationOptions
    {
        public IEnumerable<string> Defines { get; }

        public string LanguageVersion { get; }

        public string Platform { get; }

        public bool? AllowUnsafe { get; }

        public bool? WarningsAsErrors { get; }

        public bool? Optimize { get; }

        public string KeyFile { get; }

        public bool? DelaySign { get; }

        public bool? PublicSign { get; }

        public string DebugType { get; }    

        public bool? EmitEntryPoint { get; }

        public bool? GenerateXmlDocumentation { get; }

        public static CompilationOptions Default { get; } = new CompilationOptions(
            defines: Enumerable.Empty<string>(),
            languageVersion: null,
            platform: null,
            allowUnsafe: null,
            warningsAsErrors: null,
            optimize: null,
            keyFile: null,
            delaySign: null,
            publicSign: null,
            debugType: null,
            emitEntryPoint: null,
            generateXmlDocumentation: null);

        public CompilationOptions(IEnumerable<string> defines,
            string languageVersion,
            string platform,
            bool? allowUnsafe,
            bool? warningsAsErrors,
            bool? optimize,
            string keyFile,
            bool? delaySign,
            bool? publicSign,
            string debugType,
            bool? emitEntryPoint,
            bool? generateXmlDocumentation)
        {
            Defines = defines;
            LanguageVersion = languageVersion;
            Platform = platform;
            AllowUnsafe = allowUnsafe;
            WarningsAsErrors = warningsAsErrors;
            Optimize = optimize;
            KeyFile = keyFile;
            DelaySign = delaySign;
            PublicSign = publicSign;
            DebugType = debugType;
            EmitEntryPoint = emitEntryPoint;
            GenerateXmlDocumentation = generateXmlDocumentation;
        }
    }
}

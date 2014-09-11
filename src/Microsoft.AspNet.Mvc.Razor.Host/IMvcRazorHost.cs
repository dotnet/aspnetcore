// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Razor;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the contracts for a Razor host that parses Razor files and generates C# code.
    /// </summary>
    public interface IMvcRazorHost
    {
        /// <summary>
        /// Flag that indicates if page execution instrumentation code should be injected into the output.
        /// </summary>
        bool EnableInstrumentation { get; set; }

        /// <summary>
        /// Parses and generates the contents of a Razor file represented by <paramref name="inputStream"/>.
        /// </summary>
        /// <param name="rootRelativePath">The path of the relative to the root of the application. 
        /// Used to generate line pragmas and calculate the class name of the generated type.</param>
        /// <param name="inputStream">A <see cref="Stream"/> that represents the Razor contents.</param>
        /// <returns>A <see cref="GeneratorResults"/> instance that represents the results of code generation.
        /// </returns>
        GeneratorResults GenerateCode(string rootRelativePath, Stream inputStream);

        /// <summary>
        /// Represent the prefix off the main entry class in the view.
        /// </summary>
        string MainClassNamePrefix { get; }

        /// <summary>
        /// Represent the namespace the main entry class in the view.
        /// </summary>
        string DefaultNamespace { get; }
    }
}

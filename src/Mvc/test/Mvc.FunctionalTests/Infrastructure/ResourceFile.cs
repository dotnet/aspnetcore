// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Reader and, if GenerateBaselines is set to true, writer for files compiled into an assembly as resources.
/// </summary>
/// <remarks>Inspired by Razor's BaselineWriter and TestFile test classes.</remarks>
public static class ResourceFile
{
    /// <summary>
    /// Set this to true to cause baseline files to becompiled into an assembly as resources.
    /// </summary>
    public static readonly bool GenerateBaselines = false;

    private static readonly object writeLock = new object();

    public static void UpdateOrVerify(Assembly assembly, string outputFile, string expectedContent, string responseContent, string token = null)
    {
        if (GenerateBaselines)
        {
            // Reverse usual substitution and insert a format item into the new file content.
            if (token != null)
            {
                responseContent = responseContent.Replace(token, "{0}");
            }
            UpdateFile(assembly, outputFile, expectedContent, responseContent);
        }
        else
        {
            if (token != null)
            {
                expectedContent = string.Format(CultureInfo.InvariantCulture, expectedContent, token);
            }
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
        }
    }

    /// <summary>
    /// Return <see cref="Stream"/> for <paramref name="resourceName"/> from <paramref name="assembly"/>'s
    /// manifest.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
    /// <param name="resourceName">
    /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
    /// directory.
    /// </param>
    /// <param name="sourceFile">
    /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
    /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
    /// soon be generated if missing.
    /// </param>
    /// <returns>
    /// <see cref="Stream"/> for <paramref name="resourceName"/> from <paramref name="assembly"/>'s
    /// manifest. <c>null</c> if <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is
    /// <c>false</c>, and <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </returns>
    /// <exception cref="Xunit.Sdk.TrueException">
    /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
    /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </exception>
    public static Stream GetResourceStream(Assembly assembly, string resourceName, bool sourceFile)
    {
        var fullName = $"{ assembly.GetName().Name }.{ resourceName.Replace('/', '.') }";
        if (!Exists(assembly, fullName))
        {
            if (GenerateBaselines)
            {
                if (sourceFile)
                {
                    // Even when generating baselines, a missing source file is a serious problem.
                    Assert.True(false, $"Manifest resource: { fullName } not found.");
                }
            }
            else
            {
                // When not generating baselines, a missing source or output file is always an error.
                Assert.True(false, $"Manifest resource '{ fullName }' not found.");
            }

            return null;
        }

        var stream = assembly.GetManifestResourceStream(fullName);
        if (sourceFile)
        {
            // Normalize line endings to '\r\n' (CRLF). This removes core.autocrlf, core.eol, core.safecrlf, and
            // .gitattributes from the equation and treats "\r\n" and "\n" as equivalent. Does not handle
            // some line endings like "\r" but otherwise ensures checksums and line mappings are consistent.
            string text;
            using (var streamReader = new StreamReader(stream))
            {
                text = streamReader.ReadToEnd().Replace("\r", "").Replace("\n", "\r\n");
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            stream = new MemoryStream(bytes);
        }

        return stream;
    }

    /// <summary>
    /// Return <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
    /// manifest.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
    /// <param name="resourceName">
    /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
    /// directory.
    /// </param>
    /// <param name="sourceFile">
    /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
    /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
    /// soon be generated if missing.
    /// </param>
    /// <returns>
    /// A <see cref="Task{string}"/> which on completion returns the <see cref="string"/> content of
    /// <paramref name="resourceName"/> from <paramref name="assembly"/>'s manifest. <c>null</c> if
    /// <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is <c>false</c>, and
    /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </returns>
    /// <exception cref="Xunit.Sdk.TrueException">
    /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
    /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </exception>
    /// <remarks>Normalizes line endings to <see cref="Environment.NewLine"/>.</remarks>
    public static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, bool sourceFile)
    {
        using (var stream = GetResourceStream(assembly, resourceName, sourceFile))
        {
            if (stream == null)
            {
                return null;
            }

            using (var streamReader = new StreamReader(stream))
            {
                return await streamReader.ReadToEndAsync();
            }
        }
    }

    /// <summary>
    /// Return <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
    /// manifest.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
    /// <param name="resourceName">
    /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
    /// directory.
    /// </param>
    /// <param name="sourceFile">
    /// If <c>true</c> <paramref name="resourceName"/> is used as a source file and must exist. Otherwise
    /// <paramref name="resourceName"/> is an output file and, if <c>GENERATE_BASELINES</c> is defined, it will
    /// soon be generated if missing.
    /// </param>
    /// <returns>
    /// The <see cref="string"/> content of <paramref name="resourceName"/> from <paramref name="assembly"/>'s
    /// manifest. <c>null</c> if <c>GENERATE_BASELINES</c> is defined, <paramref name="sourceFile"/> is
    /// <c>false</c>, and <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </returns>
    /// <exception cref="Xunit.Sdk.TrueException">
    /// Thrown if <c>GENERATE_BASELINES</c> is not defined or <paramref name="sourceFile"/> is <c>true</c> and
    /// <paramref name="resourceName"/> is not found in <paramref name="assembly"/>.
    /// </exception>
    /// <remarks>Normalizes line endings to <see cref="Environment.NewLine"/>.</remarks>
    public static string ReadResource(Assembly assembly, string resourceName, bool sourceFile)
    {
        using (var stream = GetResourceStream(assembly, resourceName, sourceFile))
        {
            if (stream == null)
            {
                return null;
            }

            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }
    }

    /// <summary>
    /// Write <paramref name="content"/> to file that will become <paramref name="resourceName"/> in
    /// <paramref name="assembly"/> the next time the project is built. Does nothing if
    /// <paramref name="previousContent"/> and <paramref name="content"/> already match.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> containing <paramref name="resourceName"/>.</param>
    /// <param name="resourceName">
    /// Name of the manifest resource in <paramref name="assembly"/>. A path relative to the test project
    /// directory.
    /// </param>
    /// <param name="previousContent">
    /// Current content of <paramref name="resourceName"/>. <c>null</c> if <paramref name="resourceName"/> does
    /// not currently exist in <paramref name="assembly"/>.
    /// </param>
    /// <param name="content">
    /// New content of <paramref name="resourceName"/> in <paramref name="assembly"/>.
    /// </param>
    private static void UpdateFile(Assembly assembly, string resourceName, string previousContent, string content)
    {
        if (!GenerateBaselines)
        {
            throw new NotSupportedException("Calling UpdateFile is not supported when GenerateBaselines=false");
        }

        // Normalize line endings to '\r\n' for comparison. This removes Environment.NewLine from the equation. Not
        // worth updating files just because we generate baselines on a different system.
        var normalizedPreviousContent = previousContent?.Replace("\r", "").Replace("\n", "\r\n");
        var normalizedContent = content.Replace("\r", "").Replace("\n", "\r\n");

        if (!string.Equals(normalizedPreviousContent, normalizedContent, StringComparison.Ordinal))
        {
            // The build system compiles every file under the resources folder as a resource available at runtime
            // with the same name as the file name. Need to update this file on disc.

            // https://github.com/dotnet/aspnetcore/issues/10423
#pragma warning disable 0618
            var solutionPath = TestPathUtilities.GetSolutionRootDirectory("Mvc");
#pragma warning restore 0618
            var projectPath = Path.Combine(solutionPath, "test", assembly.GetName().Name);
            var fullPath = Path.Combine(projectPath, resourceName);
            WriteFile(fullPath, content);
        }
    }

    private static bool Exists(Assembly assembly, string fullName)
    {
        var resourceNames = assembly.GetManifestResourceNames();
        foreach (var resourceName in resourceNames)
        {
            // Resource names are case-sensitive.
            if (string.Equals(fullName, resourceName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteFile(string fullPath, string content)
    {
        // Serialize writes to minimize contention for file handles and directory access.
        lock (writeLock)
        {
            // Write content to the file, creating it if necessary.
            using (var stream = File.Open(fullPath, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                }
            }
        }
    }
}

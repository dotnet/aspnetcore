// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RepoTasks;

internal static partial class FileUtilities
{
    private static readonly HashSet<string> s_assemblyExtensions = new HashSet<string>(
        new[] { ".dll", ".exe", ".winmd" },
        StringComparer.OrdinalIgnoreCase);

    public static Version GetFileVersion(string sourcePath)
    {
        var fvi = FileVersionInfo.GetVersionInfo(sourcePath);

        if (fvi != null)
        {
            return new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
        }

        return null;
    }

    public static AssemblyName GetAssemblyName(string path)
    {
        if (!s_assemblyExtensions.Contains(Path.GetExtension(path)))
        {
            return null;
        }

        try
        {
            return AssemblyName.GetAssemblyName(path);
        }
        catch (BadImageFormatException)
        {
            // Not a valid assembly.
            return null;
        }
    }
}

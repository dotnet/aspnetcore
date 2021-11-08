// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class FileKinds
{
    public static readonly string Component = "component";

    public static readonly string ComponentImport = "componentImport";

    public static readonly string Legacy = "mvc";

    public static bool IsComponent(string fileKind)
    {
        // fileKind might be null.
        return string.Equals(fileKind, FileKinds.Component, StringComparison.OrdinalIgnoreCase) || IsComponentImport(fileKind);
    }

    public static bool IsComponentImport(string fileKind)
    {
        // fileKind might be null.
        return string.Equals(fileKind, FileKinds.ComponentImport, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetComponentFileKindFromFilePath(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal))
        {
            return FileKinds.ComponentImport;
        }
        else
        {
            return FileKinds.Component;
        }
    }

    public static string GetFileKindFromFilePath(string filePath)
    {
        if (filePath == null)
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (string.Equals(ComponentMetadata.ImportsFileName, Path.GetFileName(filePath), StringComparison.Ordinal))
        {
            return FileKinds.ComponentImport;
        }
        else if (string.Equals(".razor", Path.GetExtension(filePath), StringComparison.OrdinalIgnoreCase))
        {
            return FileKinds.Component;
        }
        else
        {
            return FileKinds.Legacy;
        }
    }
}

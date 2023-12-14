// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.InternalTesting;

public static class HelixHelper
{
    public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());

    public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");

    // Uploads the file on helix, or puts the file in your user temp folder when running locally
    public static void PreserveFile(string filePath, string uploadFileName)
    {
        var uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        var uploadPath = string.IsNullOrEmpty(uploadRoot)
            ? Path.Combine(Path.GetTempPath(), uploadFileName)
            : Path.Combine(uploadRoot, uploadFileName);
        File.Copy(filePath, uploadPath, overwrite: true);
    }
}

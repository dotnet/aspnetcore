// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ConcatenateCssFiles : Task
    {
        [Required]
        public ITaskItem[] FilesToProcess { get; set; }

        [Required]
        public string OutputFile { get; set; }

        public override bool Execute()
        {
            var builder = new StringBuilder();
            var orderedFiles = FilesToProcess.OrderBy(f => f.GetMetadata("FullPath")).ToArray();
            for (var i = 0; i < orderedFiles.Length; i++)
            {
                var current = orderedFiles[i];
                builder.AppendLine($"/* {current.GetMetadata("BasePath").Replace("\\","/")}{current.GetMetadata("RelativePath").Replace("\\","/")} */");
                foreach (var line in File.ReadLines(FilesToProcess[i].GetMetadata("FullPath")))
                {
                    builder.AppendLine(line);
                }
            }

            var content = builder.ToString();

            if (!File.Exists(OutputFile) || !SameContent(content, OutputFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
                File.WriteAllText(OutputFile, content);
            }


            return !Log.HasLoggedErrors;
        }

        private bool SameContent(string content, string outputFilePath)
        {
            var contentHash = GetContentHash(content);

            var outputContent = File.ReadAllText(outputFilePath);
            var outputContentHash = GetContentHash(outputContent);

            for (int i = 0; i < outputContentHash.Length; i++)
            {
                if (outputContentHash[i] != contentHash[i])
                {
                    return false;
                }
            }

            return true;

            static byte[] GetContentHash(string content)
            {
                using var sha256 = SHA256.Create();
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            }
        }
    }
}

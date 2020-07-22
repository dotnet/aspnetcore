// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class ConcatenateCssFilesCommand : CommandBase
    {
        public ConcatenateCssFilesCommand(Application parent)
            : base(parent, "concatenatecss")
        {
            Sources = Option("-s", "Files to concatenate", CommandOptionType.MultipleValue);
            Output = Option("-o", "Output file path", CommandOptionType.SingleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Output { get; }

        protected override async Task<int> ExecuteCoreAsync()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < Sources.Values.Count; i++)
            {
                builder.AppendLine();
                foreach (var line in File.ReadLines(Sources.Values[i]))
                {
                    builder.AppendLine(line);
                }
            }

            var content = builder.ToString();

            if (!File.Exists(Output.Value()) || !SameContent(content, Output.Value()))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Output.Value()));
                await File.WriteAllTextAsync(Output.Value(), content);
            }

            return ExitCodeSuccess;
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

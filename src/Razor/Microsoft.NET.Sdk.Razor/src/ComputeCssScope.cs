// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ComputeCssScope : Task
    {
        static char[] Alphabet = new char[16]{ 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p' };

        [Required]
        public ITaskItem[] ScopedCssInput { get; set; }

        [Required]
        public string TargetName { get; set; }

        [Output]
        public ITaskItem[] ScopedCss { get; set; }

        public override bool Execute()
        {
            ScopedCss = new ITaskItem[ScopedCssInput.Length];

            for (var i = 0; i < ScopedCssInput.Length; i++)
            {
                var input = ScopedCssInput[i];
                var relativePath = input.GetMetadata("RelativePath");
                var scope = input.GetMetadata("CssScope");
                scope = !string.IsNullOrEmpty(scope) ? scope : GenerateScope(TargetName, relativePath);

                var outputItem = new TaskItem(input);
                outputItem.SetMetadata("CssScope", scope);
                ScopedCss[i] = outputItem;
            }

            return !Log.HasLoggedErrors;
        }

        private string GenerateScope(string targetName, string relativePath)
        {
            using var hash = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(relativePath + targetName);
            var hashBytes = hash.ComputeHash(bytes);

            var builder = new StringBuilder();

            for (var i = 0; i < 4; i++)
            {
                var currentByte = hashBytes[i];
                builder.Append(Alphabet[currentByte & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 2 & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 4 & 0b00001111]);
                builder.Append(Alphabet[currentByte >> 6 & 0b00001111]);
            }

            return builder.ToString();
        }
    }
}

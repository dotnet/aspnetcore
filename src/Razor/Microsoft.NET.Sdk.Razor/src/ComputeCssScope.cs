// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Razor.Tasks
{
    public class ComputeCssScope : Task
    {
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
                var relativePath = input.ItemSpec.ToLowerInvariant().Replace("\\","//");
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
            builder.Append("b-");

            builder.Append(ToBase36(hashBytes));

            return builder.ToString();
        }

        private static string ToBase36(byte[] hash)
        {
            const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";

            var result = new char[10];
            var dividend = BigInteger.Abs(new BigInteger(hash.AsSpan().Slice(0, 9).ToArray()));
            for (var i = 0; i < 10; i++)
            {
                dividend = BigInteger.DivRem(dividend, 36, out var remainder);
                result[i] = chars[(int)remainder];
            }

            return new string(result);
        }
    }
}

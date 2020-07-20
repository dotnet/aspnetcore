// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.NET.Sdk.BlazorWebAssembly.Tools
{
    internal class BrotliCompressCommand : CommandLineApplication
    {
        public BrotliCompressCommand(Application parent)
            : base(throwOnUnexpectedArg: true)
        {
            base.Parent = parent;
            Name = "brotli";
            Sources = Option("-s", "files to compress", CommandOptionType.MultipleValue);
            Outputs = Option("-o", "Output file path", CommandOptionType.MultipleValue);
            CompressionLevelOption = Option("-c", "Compression level", CommandOptionType.SingleValue);

            Invoke = () => Execute().GetAwaiter().GetResult();
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption CompressionLevelOption { get; }

        public CompressionLevel CompressionLevel { get; private set; } = CompressionLevel.Optimal;

        private Task<int> Execute()
        {
            if (!ValidateArguments())
            {
                ShowHelp();
                return Task.FromResult(1);
            }

            return ExecuteCoreAsync();
        }

        private bool ValidateArguments()
        {
            if (Sources.Values.Count != Outputs.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {Outputs.Description} has {Outputs.Values.Count} values.");
                return false;
            }

            if (CompressionLevelOption.HasValue())
            {
                if (!Enum.TryParse<CompressionLevel>(CompressionLevelOption.Value(), out var value))
                {
                    Error.WriteLine($"Invalid option {CompressionLevelOption.Value()} for {CompressionLevelOption.Template}.");
                    return false;
                }

                CompressionLevel = value;
            }

            return true;
        }

        private Task<int> ExecuteCoreAsync()
        {
            Parallel.For(0, Sources.Values.Count, i =>
            {
                var source = Sources.Values[i];
                var output = Outputs.Values[i];

                using var sourceStream = File.OpenRead(source);
                using var fileStream = new FileStream(output, FileMode.Create);

                using var stream = new BrotliStream(fileStream, CompressionLevel);

                sourceStream.CopyTo(stream);
            });

            return Task.FromResult(0);
        }
    }
}

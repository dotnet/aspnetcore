// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class BrotliCompressCommand : CommandBase
    {
        public BrotliCompressCommand(Application parent)
            : base(parent, "brotli")
        {
            Sources = Option("-s", ".cshtml files to compile", CommandOptionType.MultipleValue);
            Outputs = Option("-o", "Generated output file path", CommandOptionType.MultipleValue);
            CompressionLevelOption = Option("-c", "Compression level", CommandOptionType.SingleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption CompressionLevelOption { get; }

        public CompressionLevel CompressionLevel { get; private set; } = CompressionLevel.Optimal;

        protected override bool ValidateArguments()
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

        protected override Task<int> ExecuteCoreAsync()
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

            return Task.FromResult(ExitCodeSuccess);
        }
    }
}

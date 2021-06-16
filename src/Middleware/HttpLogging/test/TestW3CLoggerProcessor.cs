// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class TestW3CLoggerProcessor : W3CLoggerProcessor
    {
        public int WriteCount = 0;
        public string[] Lines;
        private bool _hasWritten;

        public TestW3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options) : base(options)
        {
            // Never needs to be longer than 4 lines
            Lines = new string[4];
        }

        internal override StreamWriter GetStreamWriter(string fileName)
        {
            return StreamWriter.Null;
        }

        internal override void OnWrite(string message)
        {
            if (Lines.Length > WriteCount)
            {
                Lines[WriteCount] = message;
            }
            WriteCount++;
        }

        public override async Task OnFirstWrite(StreamWriter streamWriter)
        {
            if (!_hasWritten)
            {
                await base.OnFirstWrite(streamWriter);
                _hasWritten = true;
            }
        }
    }
}

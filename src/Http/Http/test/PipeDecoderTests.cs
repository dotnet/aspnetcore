using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace Microsoft.AspNetCore.Http.Tests
{
    public class PipeDecoderTests
    {
        private static readonly char[] CharData = new char[]
        {
            char.MinValue,
            char.MaxValue,
            '\t',
            ' ',
            '$',
            '@',
            '#',
            '\0',
            '\v',
            '\'',
            '\u3190',
            '\uC3A0',
            'A',
            '5',
            '\r',
            '\uFE70',
            '-',
            ';',
            '\r',
            '\n',
            'T',
            '3',
            '\n',
            'K',
            '\u00E6',
        };

        [Fact]
        public async Task ReadAsyncWorks()
        {
            var reader = CreateReader();
            var ros = await reader.ReadAsync();
            Assert.Equal(25, ros.Length);
        }

        [Fact]
        public async Task ReadAsyncThrowsIfCalledMultipleTimesWithoutAdvance()
        {
            var reader = CreateReader();
            var ros = await reader.ReadAsync();
            var ros2 = await reader.ReadAsync();
            Assert.Equal(ros, ros2);
        }

        [Fact]
        public async Task ReadAsyncWithAdvance()
        {
            var pipe = new Pipe();
            var wrapper = new WriteOnlyPipeStream(pipe.Writer);
            var writer = new StreamWriter(wrapper);
            writer.Write(CharData);
            writer.Flush();

            var reader = new PipeDecoder(pipe.Reader, Encoding.UTF8);

            var ros = await reader.ReadAsync();

            reader.AdvanceTo(ros.End);
            ros = await reader.ReadAsync(); // THis one will hang. TODO
        }

        private static PipeDecoder CreateReader()
        {
            var pipe = new Pipe();
            var wrapper = new WriteOnlyPipeStream(pipe.Writer);
            var writer = new StreamWriter(wrapper);
            writer.Write(CharData);
            writer.Flush();

            return new PipeDecoder(pipe.Reader, Encoding.UTF8);
        }

        private static async Task<PipeReader> GetLargePipeReader()
        {
            var testData = new byte[] { 72, 69, 76, 76, 79 };
            var pipe = new Pipe();

            for (var i = 0; i < 1000; i++)
            {
                await pipe.Writer.WriteAsync(testData);
            }

            return pipe.Reader;
        }
    }
}

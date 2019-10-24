using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class VariableIntHelperTests
    {
        // 0x1F * N + 0x21 is ignored.
        [Theory]
        [MemberData(nameof(IntegerData))]
        public void CheckDecoding(long expected, byte[] input)
        {
            var decoded = VariableIntHelper.GetVariableIntFromReadOnlySequence(new ReadOnlySequence<byte>(input), out _, out _);
            Assert.Equal(expected, decoded);
        }

        public static TheoryData<long, byte[]> IntegerData
        {
            get
            {
                var data = new TheoryData<long, byte[]>();

                data.Add(151288809941952652, new byte[] { 0xc2, 0x19, 0x7c, 0x5e, 0xff, 0x14, 0xe8, 0x8c });
                data.Add(494878333, new byte[] { 0x9d, 0x7f, 0x3e, 0x7d });
                data.Add(15293, new byte[] { 0x7b, 0xbd });
                data.Add(37, new byte[] { 0x25 });
                data.Add(37, new byte[] { 0x40, 0x25 });

                return data;
            }
        }
    }
}

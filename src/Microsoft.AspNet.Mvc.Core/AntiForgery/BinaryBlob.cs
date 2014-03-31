using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using Microsoft.AspNet.Security.DataProtection;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNet.Mvc
{
    // Represents a binary blob (token) that contains random data.
    // Useful for binary data inside a serialized stream.
    [DebuggerDisplay("{DebuggerString}")]
    internal sealed class BinaryBlob : IEquatable<BinaryBlob>
    {
        private readonly byte[] _data;

        // Generates a new token using a specified bit length.
        public BinaryBlob(int bitLength)
            : this(bitLength, GenerateNewToken(bitLength))
        {
        }

        // Generates a token using an existing binary value.
        public BinaryBlob(int bitLength, byte[] data)
        {
            if (bitLength < 32 || bitLength % 8 != 0)
            {
                throw new ArgumentOutOfRangeException("bitLength");
            }
            if (data == null || data.Length != bitLength / 8)
            {
                throw new ArgumentOutOfRangeException("data");
            }

            _data = data;
        }

        public int BitLength
        {
            get
            {
                return checked(_data.Length * 8);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called by debugger.")]
        private string DebuggerString
        {
            get
            {
                StringBuilder sb = new StringBuilder("0x", 2 + (_data.Length * 2));
                for (int i = 0; i < _data.Length; i++)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", _data[i]);
                }
                return sb.ToString();
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as BinaryBlob);
        }

        public bool Equals(BinaryBlob other)
        {
            if (other == null)
            {
                return false;
            }

            Contract.Assert(this._data.Length == other._data.Length);
            return AreByteArraysEqual(this._data, other._data);
        }

        public byte[] GetData()
        {
            return _data;
        }

        public override int GetHashCode()
        {
            // Since data should contain uniformly-distributed entropy, the
            // first 32 bits can serve as the hash code.
            Contract.Assert(_data != null && _data.Length >= (32 / 8));
            return BitConverter.ToInt32(_data, 0);
        }

        private static byte[] GenerateNewToken(int bitLength)
        {
            byte[] data = new byte[bitLength / 8];
            CryptRand.FillBuffer(new ArraySegment<byte>(data));
            return data;
        }

        // Need to mark it with NoInlining and NoOptimization attributes to ensure that the 
        // operation runs in constant time.
        [MethodImplAttribute(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)] 
        private static bool AreByteArraysEqual(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                return false;
            }

            bool areEqual = true;
            for (int i = 0; i < a.Length; i++)
            {
                areEqual &= (a[i] == b[i]);
            }
            return areEqual;
        }
    }
}
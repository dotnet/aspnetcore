// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection.Test.Shared;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.SP800_108
{
    public unsafe class SP800_108Tests
    {
        private delegate ISP800_108_CTR_HMACSHA512Provider ProviderFactory(byte* pbKdk, uint cbKdk);

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [Theory]
        [InlineData(512 / 8 - 1, "V47WmHzPSkdC2vkLAomIjCzZlDOAetll3yJLcSvon7LJFjJpEN+KnSNp+gIpeydKMsENkflbrIZ/3s6GkEaH")]
        [InlineData(512 / 8 + 0, "mVaFM4deXLl610CmnCteNzxgbM/VkmKznAlPauHcDBn0le06uOjAKLHx0LfoU2/Ttq9nd78Y6Nk6wArmdwJgJg==")]
        [InlineData(512 / 8 + 1, "GaHPeqdUxriFpjRtkYQYWr5/iqneD/+hPhVJQt4rXblxSpB1UUqGqL00DMU/FJkX0iMCfqUjQXtXyfks+p++Ev4=")]
        public void DeriveKeyWithContextHeader_Normal_Managed(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = Encoding.UTF8.GetBytes("kdk");
            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestManagedKeyDerivation(kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows]
        [InlineData(512 / 8 - 1, "V47WmHzPSkdC2vkLAomIjCzZlDOAetll3yJLcSvon7LJFjJpEN+KnSNp+gIpeydKMsENkflbrIZ/3s6GkEaH")]
        [InlineData(512 / 8 + 0, "mVaFM4deXLl610CmnCteNzxgbM/VkmKznAlPauHcDBn0le06uOjAKLHx0LfoU2/Ttq9nd78Y6Nk6wArmdwJgJg==")]
        [InlineData(512 / 8 + 1, "GaHPeqdUxriFpjRtkYQYWr5/iqneD/+hPhVJQt4rXblxSpB1UUqGqL00DMU/FJkX0iMCfqUjQXtXyfks+p++Ev4=")]
        public void DeriveKeyWithContextHeader_Normal_Win7(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = Encoding.UTF8.GetBytes("kdk");
            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestCngKeyDerivation((pbKdk, cbKdk) => new Win7SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk), kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows8OrLater]
        [InlineData(512 / 8 - 1, "V47WmHzPSkdC2vkLAomIjCzZlDOAetll3yJLcSvon7LJFjJpEN+KnSNp+gIpeydKMsENkflbrIZ/3s6GkEaH")]
        [InlineData(512 / 8 + 0, "mVaFM4deXLl610CmnCteNzxgbM/VkmKznAlPauHcDBn0le06uOjAKLHx0LfoU2/Ttq9nd78Y6Nk6wArmdwJgJg==")]
        [InlineData(512 / 8 + 1, "GaHPeqdUxriFpjRtkYQYWr5/iqneD/+hPhVJQt4rXblxSpB1UUqGqL00DMU/FJkX0iMCfqUjQXtXyfks+p++Ev4=")]
        public void DeriveKeyWithContextHeader_Normal_Win8(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = Encoding.UTF8.GetBytes("kdk");
            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestCngKeyDerivation((pbKdk, cbKdk) => new Win8SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk), kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [Theory]
        [InlineData(512 / 8 - 1, "rt2hM6kkQ8hAXmkHx0TU4o3Q+S7fie6b3S1LAq107k++P9v8uSYA2G+WX3pJf9ZkpYrTKD7WUIoLkgA1R9lk")]
        [InlineData(512 / 8 + 0, "RKiXmHSrWq5gkiRSyNZWNJrMR0jDyYHJMt9odOayRAE5wLSX2caINpQmfzTH7voJQi3tbn5MmD//dcspghfBiw==")]
        [InlineData(512 / 8 + 1, "KedXO0zAIZ3AfnPqY1NnXxpC3HDHIxefG4bwD3g6nWYEc5+q7pjbam71Yqj0zgHMNC9Z7BX3wS1/tajFocRWZUk=")]
        public void DeriveKeyWithContextHeader_LongKey_Managed(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = new byte[50000]; // CNG can't normally handle a 50,000 byte KDK, but we coerce it into working :)
            for (int i = 0; i < kdk.Length; i++)
            {
                kdk[i] = (byte)i;
            }

            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestManagedKeyDerivation(kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows]
        [InlineData(512 / 8 - 1, "rt2hM6kkQ8hAXmkHx0TU4o3Q+S7fie6b3S1LAq107k++P9v8uSYA2G+WX3pJf9ZkpYrTKD7WUIoLkgA1R9lk")]
        [InlineData(512 / 8 + 0, "RKiXmHSrWq5gkiRSyNZWNJrMR0jDyYHJMt9odOayRAE5wLSX2caINpQmfzTH7voJQi3tbn5MmD//dcspghfBiw==")]
        [InlineData(512 / 8 + 1, "KedXO0zAIZ3AfnPqY1NnXxpC3HDHIxefG4bwD3g6nWYEc5+q7pjbam71Yqj0zgHMNC9Z7BX3wS1/tajFocRWZUk=")]
        public void DeriveKeyWithContextHeader_LongKey_Win7(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = new byte[50000]; // CNG can't normally handle a 50,000 byte KDK, but we coerce it into working :)
            for (int i = 0; i < kdk.Length; i++)
            {
                kdk[i] = (byte)i;
            }

            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestCngKeyDerivation((pbKdk, cbKdk) => new Win7SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk), kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        // The 'numBytesRequested' parameters below are chosen to exercise code paths where
        // this value straddles the digest length of the PRF (which is hardcoded to HMACSHA512).
        [ConditionalTheory]
        [ConditionalRunTestOnlyOnWindows8OrLater]
        [InlineData(512 / 8 - 1, "rt2hM6kkQ8hAXmkHx0TU4o3Q+S7fie6b3S1LAq107k++P9v8uSYA2G+WX3pJf9ZkpYrTKD7WUIoLkgA1R9lk")]
        [InlineData(512 / 8 + 0, "RKiXmHSrWq5gkiRSyNZWNJrMR0jDyYHJMt9odOayRAE5wLSX2caINpQmfzTH7voJQi3tbn5MmD//dcspghfBiw==")]
        [InlineData(512 / 8 + 1, "KedXO0zAIZ3AfnPqY1NnXxpC3HDHIxefG4bwD3g6nWYEc5+q7pjbam71Yqj0zgHMNC9Z7BX3wS1/tajFocRWZUk=")]
        public void DeriveKeyWithContextHeader_LongKey_Win8(int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            // Arrange
            byte[] kdk = new byte[50000]; // CNG can't normally handle a 50,000 byte KDK, but we coerce it into working :)
            for (int i = 0; i < kdk.Length; i++)
            {
                kdk[i] = (byte)i;
            }

            byte[] label = Encoding.UTF8.GetBytes("label");
            byte[] contextHeader = Encoding.UTF8.GetBytes("contextHeader");
            byte[] context = Encoding.UTF8.GetBytes("context");

            // Act & assert
            TestCngKeyDerivation((pbKdk, cbKdk) => new Win8SP800_108_CTR_HMACSHA512Provider(pbKdk, cbKdk), kdk, label, contextHeader, context, numDerivedBytes, expectedDerivedSubkeyAsBase64);
        }

        private static void TestCngKeyDerivation(ProviderFactory factory, byte[] kdk, byte[] label, byte[] contextHeader, byte[] context, int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            byte[] derivedSubkey = new byte[numDerivedBytes];

            fixed (byte* pbKdk = kdk)
            fixed (byte* pbLabel = label)
            fixed (byte* pbContext = context)
            fixed (byte* pbDerivedSubkey = derivedSubkey)
            {
                ISP800_108_CTR_HMACSHA512Provider provider = factory(pbKdk, (uint)kdk.Length);
                provider.DeriveKeyWithContextHeader(pbLabel, (uint)label.Length, contextHeader, pbContext, (uint)context.Length, pbDerivedSubkey, (uint)derivedSubkey.Length);
            }

            Assert.Equal(expectedDerivedSubkeyAsBase64, Convert.ToBase64String(derivedSubkey));
        }

        private static void TestManagedKeyDerivation(byte[] kdk, byte[] label, byte[] contextHeader, byte[] context, int numDerivedBytes, string expectedDerivedSubkeyAsBase64)
        {
            var labelSegment = new ArraySegment<byte>(new byte[label.Length + 10], 3, label.Length);
            Buffer.BlockCopy(label, 0, labelSegment.Array, labelSegment.Offset, labelSegment.Count);
            var contextSegment = new ArraySegment<byte>(new byte[context.Length + 10], 5, context.Length);
            Buffer.BlockCopy(context, 0, contextSegment.Array, contextSegment.Offset, contextSegment.Count);
            var derivedSubkeySegment = new ArraySegment<byte>(new byte[numDerivedBytes + 10], 4, numDerivedBytes);

            ManagedSP800_108_CTR_HMACSHA512.DeriveKeysWithContextHeader(kdk, labelSegment, contextHeader, contextSegment,
                bytes => new HMACSHA512(bytes), derivedSubkeySegment);
            Assert.Equal(expectedDerivedSubkeyAsBase64, Convert.ToBase64String(derivedSubkeySegment.AsStandaloneArray()));
        }
    }
}

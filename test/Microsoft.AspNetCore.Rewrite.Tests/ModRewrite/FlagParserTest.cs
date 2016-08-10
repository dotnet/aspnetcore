// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite;
using Xunit;

namespace Microsoft.AspNetCore.Rewrite.Tests.ModRewrite
{
    public class FlagParserTest
    {
        [Fact]
        public void FlagParser_CheckSingleTerm()
        {
            var results = FlagParser.Parse("[NC]");
            var dict = new Dictionary<FlagType, string>();
            dict.Add(FlagType.NoCase, string.Empty);
            var expected = new Flags(dict);

            Assert.True(DictionaryContentsEqual(results.FlagDictionary, expected.FlagDictionary));
        }

        [Fact]
        public void FlagParser_CheckManyTerms()
        {
            var results = FlagParser.Parse("[NC,F,L]");
            var dict = new Dictionary<FlagType, string>();
            dict.Add(FlagType.NoCase, string.Empty);
            dict.Add(FlagType.Forbidden, string.Empty);
            dict.Add(FlagType.Last, string.Empty);
            var expected = new Flags(dict);

            Assert.True(DictionaryContentsEqual(results.FlagDictionary, expected.FlagDictionary));
        }

        [Fact]
        public void FlagParser_CheckManyTermsWithEquals()
        {
            var results = FlagParser.Parse("[NC,F,R=301]");
            var dict = new Dictionary<FlagType, string>();
            dict.Add(FlagType.NoCase, string.Empty);
            dict.Add(FlagType.Forbidden, string.Empty);
            dict.Add(FlagType.Redirect, "301");
            var expected = new Flags(dict);

            Assert.True(DictionaryContentsEqual(results.FlagDictionary, expected.FlagDictionary));
        }

        public bool DictionaryContentsEqual<TKey, TValue>(IDictionary<TKey, TValue> dictionary, IDictionary<TKey, TValue> other)
        {
            return (other ?? new Dictionary<TKey, TValue>())
                .OrderBy(kvp => kvp.Key)
                .SequenceEqual((dictionary ?? new Dictionary<TKey, TValue>())
                .OrderBy(kvp => kvp.Key));
        }
    }
}

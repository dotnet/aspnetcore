// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.TestCommon.Types;

namespace Microsoft.TestCommon
{
    public class CommonUnitTestDataSets
    {
        public static ValueTypeTestData<char> Chars { get { return TestData.CharTestData; } }
        public static ValueTypeTestData<int> Ints { get { return TestData.IntTestData; } }
        public static ValueTypeTestData<uint> Uints { get { return TestData.UintTestData; } }
        public static ValueTypeTestData<short> Shorts { get { return TestData.ShortTestData; } }
        public static ValueTypeTestData<ushort> Ushorts { get { return TestData.UshortTestData; } }
        public static ValueTypeTestData<long> Longs { get { return TestData.LongTestData; } }
        public static ValueTypeTestData<ulong> Ulongs { get { return TestData.UlongTestData; } }
        public static ValueTypeTestData<byte> Bytes { get { return TestData.ByteTestData; } }
        public static ValueTypeTestData<sbyte> SBytes { get { return TestData.SByteTestData; } }
        public static ValueTypeTestData<bool> Bools { get { return TestData.BoolTestData; } }
        public static ValueTypeTestData<double> Doubles { get { return TestData.DoubleTestData; } }
        public static ValueTypeTestData<float> Floats { get { return TestData.FloatTestData; } }
        public static ValueTypeTestData<DateTime> DateTimes { get { return TestData.DateTimeTestData; } }
        public static ValueTypeTestData<Decimal> Decimals { get { return TestData.DecimalTestData; } }
        public static ValueTypeTestData<TimeSpan> TimeSpans { get { return TestData.TimeSpanTestData; } }
        public static ValueTypeTestData<Guid> Guids { get { return TestData.GuidTestData; } }
        public static ValueTypeTestData<DateTimeOffset> DateTimeOffsets { get { return TestData.DateTimeOffsetTestData; } }
        public static ValueTypeTestData<SimpleEnum> SimpleEnums { get { return TestData.SimpleEnumTestData; } }
        public static ValueTypeTestData<LongEnum> LongEnums { get { return TestData.LongEnumTestData; } }
        public static ValueTypeTestData<FlagsEnum> FlagsEnums { get { return TestData.FlagsEnumTestData; } }
        public static TestData<string> EmptyStrings { get { return TestData.EmptyStrings; } }
        public static RefTypeTestData<string> Strings { get { return TestData.StringTestData; } }
        public static TestData<string> NonNullEmptyStrings { get { return TestData.NonNullEmptyStrings; } }
        public static RefTypeTestData<ISerializableType> ISerializableTypes { get { return TestData.ISerializableTypeTestData; } }
        public static ReadOnlyCollection<TestData> ValueTypeTestDataCollection { get { return TestData.ValueTypeTestDataCollection; } }
        public static ReadOnlyCollection<TestData> RefTypeTestDataCollection { get { return TestData.RefTypeTestDataCollection; } }
        public static ReadOnlyCollection<TestData> ValueAndRefTypeTestDataCollection { get { return TestData.ValueAndRefTypeTestDataCollection; } }
        public static ReadOnlyCollection<TestData> RepresentativeValueAndRefTypeTestDataCollection { get { return TestData.RepresentativeValueAndRefTypeTestDataCollection; } }
    }
}
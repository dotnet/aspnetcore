// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.TestCommon.Types;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// A base class for test data.  A <see cref="TestData"/> instance is associated with a given type, and the <see cref="TestData"/> instance can
    /// provide instances of the given type to use as data in tests.  The same <see cref="TestData"/> instance can also provide instances
    /// of types related to the given type, such as a <see cref="List<>"/> of the type.  See the <see cref="TestDataVariations"/> enum for all the
    /// variations of test data that a <see cref="TestData"/> instance can provide.
    /// </summary>
    public abstract class TestData
    {
        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="char"/>.
        /// </summary>
        public static readonly ValueTypeTestData<char> CharTestData = new ValueTypeTestData<char>('a', Char.MinValue, Char.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="int"/>.
        /// </summary>
        public static readonly ValueTypeTestData<int> IntTestData = new ValueTypeTestData<int>(-1, 0, 1, Int32.MinValue, Int32.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="uint"/>.
        /// </summary>
        public static readonly ValueTypeTestData<uint> UintTestData = new ValueTypeTestData<uint>(0, 1, UInt32.MinValue, UInt32.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="short"/>.
        /// </summary>
        public static readonly ValueTypeTestData<short> ShortTestData = new ValueTypeTestData<short>(-1, 0, 1, Int16.MinValue, Int16.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="ushort"/>.
        /// </summary>
        public static readonly ValueTypeTestData<ushort> UshortTestData = new ValueTypeTestData<ushort>(0, 1, UInt16.MinValue, UInt16.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="long"/>.
        /// </summary>
        public static readonly ValueTypeTestData<long> LongTestData = new ValueTypeTestData<long>(-1, 0, 1, Int64.MinValue, Int64.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="ulong"/>.
        /// </summary>
        public static readonly ValueTypeTestData<ulong> UlongTestData = new ValueTypeTestData<ulong>(0, 1, UInt64.MinValue, UInt64.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="byte"/>.
        /// </summary>
        public static readonly ValueTypeTestData<byte> ByteTestData = new ValueTypeTestData<byte>(0, 1, Byte.MinValue, Byte.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="sbyte"/>.
        /// </summary>
        public static readonly ValueTypeTestData<sbyte> SByteTestData = new ValueTypeTestData<sbyte>(-1, 0, 1, SByte.MinValue, SByte.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="bool"/>.
        /// </summary>
        public static readonly ValueTypeTestData<bool> BoolTestData = new ValueTypeTestData<bool>(true, false);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="double"/>.
        /// </summary>
        public static readonly ValueTypeTestData<double> DoubleTestData = new ValueTypeTestData<double>(
            -1.0,
            0.0,
            1.0,
            double.MinValue,
            double.MaxValue,
            double.PositiveInfinity,
            double.NegativeInfinity);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="float"/>.
        /// </summary>
        public static readonly ValueTypeTestData<float> FloatTestData = new ValueTypeTestData<float>(
            -1.0f,
            0.0f,
            1.0f,
            float.MinValue,
            float.MaxValue,
            float.PositiveInfinity,
            float.NegativeInfinity);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="decimal"/>.
        /// </summary>
        public static readonly ValueTypeTestData<decimal> DecimalTestData = new ValueTypeTestData<decimal>(
            -1M,
            0M,
            1M,
            decimal.MinValue,
            decimal.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="DateTime"/>.
        /// </summary>
        public static readonly ValueTypeTestData<DateTime> DateTimeTestData = new ValueTypeTestData<DateTime>(
            DateTime.Now,
            DateTime.UtcNow,
            DateTime.MaxValue,
            DateTime.MinValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="TimeSpan"/>.
        /// </summary>
        public static readonly ValueTypeTestData<TimeSpan> TimeSpanTestData = new ValueTypeTestData<TimeSpan>(
            TimeSpan.MinValue,
            TimeSpan.MaxValue);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="Guid"/>.
        /// </summary>
        public static readonly ValueTypeTestData<Guid> GuidTestData = new ValueTypeTestData<Guid>(
            Guid.NewGuid(),
            Guid.Empty);

        /// <summary>
        /// Common <see cref="TestData"/> for a <see cref="DateTimeOffset"/>.
        /// </summary>
        public static readonly ValueTypeTestData<DateTimeOffset> DateTimeOffsetTestData = new ValueTypeTestData<DateTimeOffset>(
            DateTimeOffset.MaxValue,
            DateTimeOffset.MinValue,
            new DateTimeOffset(DateTime.Now));

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c>.
        /// </summary>
        public static readonly ValueTypeTestData<SimpleEnum> SimpleEnumTestData = new ValueTypeTestData<SimpleEnum>(
            SimpleEnum.First,
            SimpleEnum.Second,
            SimpleEnum.Third);

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c> implemented with a <see cref="long"/>.
        /// </summary>
        public static readonly ValueTypeTestData<LongEnum> LongEnumTestData = new ValueTypeTestData<LongEnum>(
            LongEnum.FirstLong,
            LongEnum.SecondLong,
            LongEnum.ThirdLong);

        /// <summary>
        /// Common <see cref="TestData"/> for an <c>enum</c> decorated with a <see cref="FlagsAttribtute"/>.
        /// </summary>
        public static readonly ValueTypeTestData<FlagsEnum> FlagsEnumTestData = new ValueTypeTestData<FlagsEnum>(
            FlagsEnum.One,
            FlagsEnum.Two,
            FlagsEnum.Four);

        /// <summary>
        /// Expected permutations of non supported file paths.
        /// </summary>
        public static readonly TestData<string> NotSupportedFilePaths = new RefTypeTestData<string>(() => new List<string>() {
            "cc:\\a\\b",
        });

        /// <summary>
        /// Expected permutations of invalid file paths.
        /// </summary>
        public static readonly TestData<string> InvalidNonNullFilePaths = new RefTypeTestData<string>(() => new List<string>() {
            String.Empty,
            "",
            " ",
            "  ",
            "\t\t \n ",
            "c:\\a<b",
            "c:\\a>b",
            "c:\\a\"b",
            "c:\\a\tb",
            "c:\\a|b",
            "c:\\a\bb",
            "c:\\a\0b",
        });

        /// <summary>
        /// All expected permutations of an empty string.
        /// </summary>
        public static readonly TestData<string> NonNullEmptyStrings = new RefTypeTestData<string>(() => new List<string>() { String.Empty, "", " ", "\t\r\n" });

        /// <summary>
        /// All expected permutations of an empty string.
        /// </summary>
        public static readonly TestData<string> EmptyStrings = new RefTypeTestData<string>(() => new List<string>() { null, String.Empty, "", " ", "\t\r\n" });

        /// <summary>
        ///  Common <see cref="TestData"/> for a <see cref="string"/>.
        /// </summary>
        public static readonly RefTypeTestData<string> StringTestData = new RefTypeTestData<string>(() => new List<string>() {
            "",
            " ",            // one space
            "  ",           // multiple spaces
            " data ",       // leading and trailing whitespace
            "\t\t \n ",
            "Some String!"});

        /// <summary>
        /// A read-only collection of value type test data.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> ValueTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] {
            CharTestData,
            IntTestData,
            UintTestData,
            ShortTestData,
            UshortTestData,
            LongTestData,
            UlongTestData,
            ByteTestData,
            SByteTestData,
            BoolTestData,
            DoubleTestData,
            FloatTestData,
            DecimalTestData,
            TimeSpanTestData,
            GuidTestData,
            DateTimeOffsetTestData,
            SimpleEnumTestData,
            LongEnumTestData,
            FlagsEnumTestData});

        /// <summary>
        /// A read-only collection of representative values and reference type test data.
        /// Uses where exhaustive coverage is not required.
        /// </summary>
        public static readonly ReadOnlyCollection<TestData> RepresentativeValueAndRefTypeTestDataCollection = new ReadOnlyCollection<TestData>(new TestData[] {
            IntTestData,
            BoolTestData,
            SimpleEnumTestData,
            StringTestData,
        });

        private Dictionary<TestDataVariations, TestDataVariationProvider> registeredTestDataVariations;


        /// <summary>
        /// Initializes a new instance of the <see cref="TestData"/> class.
        /// </summary>
        /// <param name="type">The type associated with the <see cref="TestData"/> instance.</param>
        protected TestData(Type type)
        {
            this.Type = type;
            this.registeredTestDataVariations = new Dictionary<TestDataVariations, TestDataVariationProvider>();
        }

        /// <summary>
        /// Gets the type associated with the <see cref="TestData"/> instance.
        /// </summary>
        public Type Type { get; private set; }


        /// <summary>
        /// Gets the supported test data variations.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TestDataVariations> GetSupportedTestDataVariations()
        {
            return this.registeredTestDataVariations.Keys;
        }

        /// <summary>
        /// Gets the related type for the given test data variation or returns null if the <see cref="TestData"/> instance
        /// doesn't support the given variation.
        /// </summary>
        /// <param name="variation">The test data variation with which to create the related <see cref="Type"/>.</param>
        /// <returns>The related <see cref="Type"/> for the <see cref="TestData.Type"/> as given by the test data variation.</returns>
        /// <example>
        /// For example, if the given <see cref="TestData"/> was created for <see cref="string"/> test data and the variation parameter
        /// was <see cref="TestDataVariations.AsList"/> then the returned type would be <see cref="List<string>"/>.
        /// </example>
        public Type GetAsTypeOrNull(TestDataVariations variation)
        {
            TestDataVariationProvider testDataVariation = null;
            if (this.registeredTestDataVariations.TryGetValue(variation, out testDataVariation))
            {
                return testDataVariation.Type;
            }

            return null;
        }

        /// <summary>
        /// Gets test data for the given test data variation or returns null if the <see cref="TestData"/> instance
        /// doesn't support the given variation.
        /// </summary>
        /// <param name="variation">The test data variation with which to create the related test data.</param>
        /// <returns>Test data of the type specified by the <see cref="TestData.GetAsTypeOrNull"/> method.</returns>
        public object GetAsTestDataOrNull(TestDataVariations variation)
        {
            TestDataVariationProvider testDataVariation = null;
            if (this.registeredTestDataVariations.TryGetValue(variation, out testDataVariation))
            {
                return testDataVariation.TestDataProvider();
            }

            return null;
        }


        /// <summary>
        /// Allows derived classes to register a <paramref name="testDataProvider "/> <see cref="Func<>"/> that will
        /// provide test data for a given variation.
        /// </summary>
        /// <param name="variation">The variation with which to register the <paramref name="testDataProvider "/>r.</param>
        /// <param name="type">The type of the test data created by the <paramref name="testDataProvider "/></param>
        /// <param name="testDataProvider">A <see cref="Func<>"/> that will provide test data.</param>
        protected void RegisterTestDataVariation(TestDataVariations variation, Type type, Func<object> testDataProvider)
        {
            this.registeredTestDataVariations.Add(variation, new TestDataVariationProvider(type, testDataProvider));
        }

        private class TestDataVariationProvider
        {
            public TestDataVariationProvider(Type type, Func<object> testDataProvider)
            {
                this.Type = type;
                this.TestDataProvider = testDataProvider;
            }


            public Func<object> TestDataProvider { get; private set; }

            public Type Type { get; private set; }
        }
    }


    /// <summary>
    /// A generic base class for test data.
    /// </summary>
    /// <typeparam name="T">The type associated with the test data.</typeparam>
    public abstract class TestData<T> : TestData, IEnumerable<T>
    {
        private static readonly Type OpenIEnumerableType = typeof(IEnumerable<>);
        private static readonly Type OpenListType = typeof(List<>);
        private static readonly Type OpenIQueryableType = typeof(IQueryable<>);
        private static readonly Type OpenDictionaryType = typeof(Dictionary<,>);
        private static readonly Type OpenTestDataHolderType = typeof(TestDataHolder<>);
        private int dictionaryKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestData&lt;T&gt;"/> class.
        /// </summary>
        protected TestData()
            : base(typeof(T))
        {
            Type[] typeParams = new Type[] { this.Type };
            Type[] dictionaryTypeParams = new Type[] { typeof(string), this.Type };

            Type arrayType = this.Type.MakeArrayType();
            Type listType = OpenListType.MakeGenericType(typeParams);
            Type iEnumerableType = OpenIEnumerableType.MakeGenericType(typeParams);
            Type iQueryableType = OpenIQueryableType.MakeGenericType(typeParams);
            Type dictionaryType = OpenDictionaryType.MakeGenericType(dictionaryTypeParams);
            Type testDataHolderType = OpenTestDataHolderType.MakeGenericType(typeParams);

            this.RegisterTestDataVariation(TestDataVariations.AsInstance, this.Type, () => GetTypedTestData());
            this.RegisterTestDataVariation(TestDataVariations.AsArray, arrayType, GetTestDataAsArray);
            this.RegisterTestDataVariation(TestDataVariations.AsIEnumerable, iEnumerableType, GetTestDataAsIEnumerable);
            this.RegisterTestDataVariation(TestDataVariations.AsIQueryable, iQueryableType, GetTestDataAsIQueryable);
            this.RegisterTestDataVariation(TestDataVariations.AsList, listType, GetTestDataAsList);
            this.RegisterTestDataVariation(TestDataVariations.AsDictionary, dictionaryType, GetTestDataAsDictionary);
            this.RegisterTestDataVariation(TestDataVariations.AsClassMember, testDataHolderType, GetTestDataInHolder);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)this.GetTypedTestData().ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetTypedTestData().ToList().GetEnumerator();
        }

        /// <summary>
        /// Gets the test data as an array.
        /// </summary>
        /// <returns>An array of test data of the given type.</returns>
        public T[] GetTestDataAsArray()
        {
            return this.GetTypedTestData().ToArray();
        }

        /// <summary>
        /// Gets the test data as a <see cref="List<>"/>.
        /// </summary>
        /// <returns>A <see cref="List<>"/> of test data of the given type.</returns>
        public List<T> GetTestDataAsList()
        {
            return this.GetTypedTestData().ToList();
        }

        /// <summary>
        /// Gets the test data as an <see cref="IEnumerable<>"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable<>"/> of test data of the given type.</returns>
        public IEnumerable<T> GetTestDataAsIEnumerable()
        {
            return this.GetTypedTestData().AsEnumerable();
        }

        /// <summary>
        /// Gets the test data as an <see cref="IQueryable<>"/>.
        /// </summary>
        /// <returns>An <see cref="IQueryable<>"/> of test data of the given type.</returns>
        public IQueryable<T> GetTestDataAsIQueryable()
        {
            //return this.GetTypedTestData().AsQueryable();
            return null;
        }

        public Dictionary<string, T> GetTestDataAsDictionary()
        {
            // Some TestData collections contain duplicates e.g. UintTestData contains both 0 and UInt32.MinValue.
            // Therefore use dictionaryKey, not _unused.ToString().  Reset key to keep dictionaries consistent if used
            // multiple times.
            dictionaryKey = 0;
            return this.GetTypedTestData().ToDictionary(_unused => (dictionaryKey++).ToString());
        }

        public IEnumerable<TestDataHolder<T>> GetTestDataInHolder()
        {
            return this.GetTypedTestData().Select(value => new TestDataHolder<T> { V1 = value, });
        }

        /// <summary>
        /// Must be implemented by derived types to return test data of the given type.
        /// </summary>
        /// <returns>Test data of the given type.</returns>
        protected abstract IEnumerable<T> GetTypedTestData();
    }
}

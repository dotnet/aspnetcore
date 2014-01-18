// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.TestCommon
{
    public class TestDataSetAttribute : DataAttribute
    {
        public Type DeclaringType { get; set; }

        public string PropertyName { get; set; }

        public TestDataVariations TestDataVariations { get; set; }

        private IEnumerable<Tuple<Type, string>> ExtraDataSets { get; set; }

        public TestDataSetAttribute(Type declaringType, string propertyName, TestDataVariations testDataVariations = TestCommon.TestDataVariations.All)
        {
            DeclaringType = declaringType;
            PropertyName = propertyName;
            TestDataVariations = testDataVariations;
            ExtraDataSets = new List<Tuple<Type, string>>();
        }

        public TestDataSetAttribute(Type declaringType, string propertyName,
                                    Type declaringType1, string propertyName1,
                                    TestDataVariations testDataVariations = TestCommon.TestDataVariations.All)
            : this(declaringType, propertyName, testDataVariations)
        {
            ExtraDataSets = new List<Tuple<Type, string>> { Tuple.Create(declaringType1, propertyName1) };
        }

        public TestDataSetAttribute(Type declaringType, string propertyName,
            Type declaringType1, string propertyName1,
            Type declaringType2, string propertyName2,
            TestDataVariations testDataVariations = TestCommon.TestDataVariations.All)
            : this(declaringType, propertyName, testDataVariations)
        {
            ExtraDataSets = new List<Tuple<Type, string>> { Tuple.Create(declaringType1, propertyName1), Tuple.Create(declaringType2, propertyName2) };
        }

        public TestDataSetAttribute(Type declaringType, string propertyName,
            Type declaringType1, string propertyName1,
            Type declaringType2, string propertyName2,
            Type declaringType3, string propertyName3,
            TestDataVariations testDataVariations = TestCommon.TestDataVariations.All)
            : this(declaringType, propertyName, testDataVariations)
        {
            ExtraDataSets = new List<Tuple<Type, string>> { Tuple.Create(declaringType1, propertyName1), Tuple.Create(declaringType2, propertyName2), Tuple.Create(declaringType3, propertyName3) };
        }

        public TestDataSetAttribute(Type declaringType, string propertyName,
            Type declaringType1, string propertyName1,
            Type declaringType2, string propertyName2,
            Type declaringType3, string propertyName3,
            Type declaringType4, string propertyName4,
            TestDataVariations testDataVariations = TestCommon.TestDataVariations.All)
            : this(declaringType, propertyName, testDataVariations)
        {
            ExtraDataSets = new List<Tuple<Type, string>> 
            { 
                Tuple.Create(declaringType1, propertyName1), Tuple.Create(declaringType2, propertyName2), 
                Tuple.Create(declaringType3, propertyName3), Tuple.Create(declaringType4, propertyName4) 
            };
        }

        public override IEnumerable<object[]> GetData(MethodInfo methodUnderTest, Type[] parameterTypes)
        {
            IEnumerable<object[]> baseDataSet = GetBaseDataSet(DeclaringType, PropertyName, TestDataVariations);
            IEnumerable<IEnumerable<object[]>> extraDataSets = GetExtraDataSets();

            IEnumerable<IEnumerable<object[]>> finalDataSets = (new[] { baseDataSet }).Concat(extraDataSets);

            var datasets = CrossProduct(finalDataSets);

            return datasets;
        }

        private static IEnumerable<object[]> CrossProduct(IEnumerable<IEnumerable<object[]>> datasets)
        {
            if (datasets.Count() == 1)
            {
                foreach (var dataset in datasets.First())
                {
                    yield return dataset;
                }
            }
            else
            {
                IEnumerable<object[]> datasetLeft = datasets.First();
                IEnumerable<object[]> datasetRight = CrossProduct(datasets.Skip(1));

                foreach (var dataLeft in datasetLeft)
                {
                    foreach (var dataRight in datasetRight)
                    {
                        yield return dataLeft.Concat(dataRight).ToArray();
                    }
                }
            }
        }

        // The base data set(first one) can either be a TestDataSet or a TestDataSetCollection
        private static IEnumerable<object[]> GetBaseDataSet(Type declaringType, string propertyName, TestDataVariations variations)
        {
            return TryGetDataSetFromTestDataCollection(declaringType, propertyName, variations) ?? GetDataSet(declaringType, propertyName);
        }

        private IEnumerable<IEnumerable<object[]>> GetExtraDataSets()
        {
            foreach (var tuple in ExtraDataSets)
            {
                yield return GetDataSet(tuple.Item1, tuple.Item2);
            }
        }

        private static object GetTestDataPropertyValue(Type declaringType, string propertyName)
        {
            PropertyInfo property = declaringType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);

            if (property == null)
            {
                throw new ArgumentException(String.Format("Could not find public static property {0} on {1}", propertyName, declaringType.FullName));
            }
            else
            {
                return property.GetValue(null, null);
            }
        }

        private static IEnumerable<object[]> GetDataSet(Type declaringType, string propertyName)
        {
            object propertyValue = GetTestDataPropertyValue(declaringType, propertyName);

            // box the dataset items if the property is not a RefTypeTestData 
            IEnumerable<object> value = (propertyValue as IEnumerable<object>) ?? (propertyValue as IEnumerable).Cast<object>();
            if (value == null)
            {
                throw new InvalidOperationException(String.Format("{0}.{1} is either null or does not implement IEnumerable", declaringType.FullName, propertyName));
            }

            IEnumerable<object[]> dataset = value as IEnumerable<object[]>;
            if (dataset != null)
            {
                return dataset;
            }
            else
            {
                return value.Select((data) => new object[] { data });
            }
        }

        private static IEnumerable<object[]> TryGetDataSetFromTestDataCollection(Type declaringType, string propertyName, TestDataVariations variations)
        {
            object propertyValue = GetTestDataPropertyValue(declaringType, propertyName);

            IEnumerable<TestData> testDataCollection = propertyValue as IEnumerable<TestData>;

            return testDataCollection == null ? null : GetDataSetFromTestDataCollection(testDataCollection, variations);
        }

        private static IEnumerable<object[]> GetDataSetFromTestDataCollection(IEnumerable<TestData> testDataCollection, TestDataVariations variations)
        {
            foreach (TestData testdataInstance in testDataCollection)
            {
                foreach (TestDataVariations variation in testdataInstance.GetSupportedTestDataVariations())
                {
                    if ((variation & variations) == variation)
                    {
                        Type variationType = testdataInstance.GetAsTypeOrNull(variation);
                        object testData = testdataInstance.GetAsTestDataOrNull(variation);
                        if (AsSingleInstances(variation))
                        {
                            foreach (object obj in (IEnumerable)testData)
                            {
                                yield return new object[] { variationType, obj };
                            }
                        }
                        else
                        {
                            yield return new object[] { variationType, testData };
                        }
                    }
                }
            }
        }

        private static bool AsSingleInstances(TestDataVariations variation)
        {
            return variation == TestDataVariations.AsInstance ||
                   variation == TestDataVariations.AsNullable ||
                   variation == TestDataVariations.AsDerivedType ||
                   variation == TestDataVariations.AsKnownType ||
                   variation == TestDataVariations.AsDataMember ||
                   variation == TestDataVariations.AsClassMember ||
                   variation == TestDataVariations.AsXmlElementProperty;
        }
    }
}

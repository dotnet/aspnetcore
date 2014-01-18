// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TestCommon
{
    public class ValueTypeTestData<T> : TestData<T> where T : struct
    {
        private static readonly Type OpenNullableType = typeof(Nullable<>);
        private T[] testData;

        public ValueTypeTestData(params T[] testData)
            : base()
        {
            this.testData = testData;

            Type[] typeParams = new Type[] { this.Type };
            this.RegisterTestDataVariation(TestDataVariations.WithNull, OpenNullableType.MakeGenericType(typeParams), GetNullTestData);
            this.RegisterTestDataVariation(TestDataVariations.AsNullable, OpenNullableType.MakeGenericType(typeParams), GetTestDataAsNullable);
        }

        public object GetNullTestData()
        {
            return null;
        }

        public IEnumerable<Nullable<T>> GetTestDataAsNullable()
        {
            return this.GetTypedTestData().Select(d => new Nullable<T>(d));
        }

        protected override IEnumerable<T> GetTypedTestData()
        {
            return this.testData;
        }
    }
}

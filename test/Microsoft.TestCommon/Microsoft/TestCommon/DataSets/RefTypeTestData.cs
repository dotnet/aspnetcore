// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.TestCommon
{
    public class RefTypeTestData<T> : TestData<T> where T : class
    {
        private Func<IEnumerable<T>> testDataProvider;
        private Func<IEnumerable<T>> derivedTypeTestDataProvider;
        private Func<IEnumerable<T>> knownTypeTestDataProvider;

        public RefTypeTestData(Func<IEnumerable<T>> testDataProvider)
        {
            if (testDataProvider == null)
            {
                throw new ArgumentNullException("testDataProvider");
            }

            this.testDataProvider = testDataProvider;
            this.RegisterTestDataVariation(TestDataVariations.WithNull, this.Type, GetNullTestData);
        }

        public RefTypeTestData(
            Func<IEnumerable<T>> testDataProvider,
            Func<IEnumerable<T>> derivedTypeTestDataProvider,
            Func<IEnumerable<T>> knownTypeTestDataProvider)
            : this(testDataProvider)
        {
            this.derivedTypeTestDataProvider = derivedTypeTestDataProvider;
            if (this.derivedTypeTestDataProvider != null)
            {
                this.RegisterTestDataVariation(TestDataVariations.AsDerivedType, this.Type, this.GetTestDataAsDerivedType);
            }

            this.knownTypeTestDataProvider = knownTypeTestDataProvider;
            if (this.knownTypeTestDataProvider != null)
            {
                this.RegisterTestDataVariation(TestDataVariations.AsKnownType, this.Type, this.GetTestDataAsDerivedKnownType);
            }
        }

        public T GetNullTestData()
        {
            return null;
        }

        public IEnumerable<T> GetTestDataAsDerivedType()
        {
            if (this.derivedTypeTestDataProvider != null)
            {
                return this.derivedTypeTestDataProvider();
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> GetTestDataAsDerivedKnownType()
        {
            if (this.knownTypeTestDataProvider != null)
            {
                return this.knownTypeTestDataProvider();
            }

            return Enumerable.Empty<T>();
        }

        protected override IEnumerable<T> GetTypedTestData()
        {
            return this.testDataProvider();
        }
    }
}

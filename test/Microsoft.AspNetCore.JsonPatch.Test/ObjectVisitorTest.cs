// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ObjectVisitorTest
    {
        private class Class1
        {
            public string Name { get; set; }
            public IList<string> States { get; set; } = new List<string>();
            public IDictionary<string, string> Countries = new Dictionary<string, string>();
            public dynamic Items { get; set; } = new ExpandoObject();
        }

        private class Class1Nested
        {
            public List<Class1> Customers { get; set; } = new List<Class1>();
        }

        public static IEnumerable<object[]> ReturnsListAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] { model, "/States/-", model.States };
                yield return new object[] { model.States, "/-", model.States };

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] { nestedModel, "/Customers/0/States/-", nestedModel.Customers[0].States };
                yield return new object[] { nestedModel, "/Customers/0/States/0", nestedModel.Customers[0].States };
                yield return new object[] { nestedModel.Customers, "/0/States/-", nestedModel.Customers[0].States };
                yield return new object[] { nestedModel.Customers[0], "/States/-", nestedModel.Customers[0].States };
            }
        }

        [Theory]
        [MemberData(nameof(ReturnsListAdapterData))]
        public void Visit_ValidPathToArray_ReturnsListAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath(path), new DefaultContractResolver());
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<ListAdapter>(adapter);
        }

        public static IEnumerable<object[]> ReturnsDictionaryAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] { model, "/Countries/USA", model.Countries };
                yield return new object[] { model.Countries, "/USA", model.Countries };

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] { nestedModel, "/Customers/0/Countries/USA", nestedModel.Customers[0].Countries };
                yield return new object[] { nestedModel.Customers, "/0/Countries/USA", nestedModel.Customers[0].Countries };
                yield return new object[] { nestedModel.Customers[0], "/Countries/USA", nestedModel.Customers[0].Countries };
            }
        }

        [Theory]
        [MemberData(nameof(ReturnsDictionaryAdapterData))]
        public void Visit_ValidPathToDictionary_ReturnsDictionaryAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath(path), new DefaultContractResolver());
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<DictionaryAdapter>(adapter);
        }

        public static IEnumerable<object[]> ReturnsExpandoAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] { model, "/Items/Name", model.Items };
                yield return new object[] { model.Items, "/Name", model.Items };

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] { nestedModel, "/Customers/0/Items/Name", nestedModel.Customers[0].Items };
                yield return new object[] { nestedModel.Customers, "/0/Items/Name", nestedModel.Customers[0].Items };
                yield return new object[] { nestedModel.Customers[0], "/Items/Name", nestedModel.Customers[0].Items };
            }
        }

        [Theory]
        [MemberData(nameof(ReturnsExpandoAdapterData))]
        public void Visit_ValidPathToExpandoObject_ReturnsExpandoAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath(path), new DefaultContractResolver());
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<ExpandoObjectAdapter>(adapter);
        }

        public static IEnumerable<object[]> ReturnsPocoAdapterData
        {
            get
            {
                var model = new Class1();
                yield return new object[] { model, "/Name", model };

                var nestedModel = new Class1Nested();
                nestedModel.Customers.Add(new Class1());
                yield return new object[] { nestedModel, "/Customers/0/Name", nestedModel.Customers[0] };
                yield return new object[] { nestedModel.Customers, "/0/Name", nestedModel.Customers[0] };
                yield return new object[] { nestedModel.Customers[0], "/Name", nestedModel.Customers[0] };
            }
        }

        [Theory]
        [MemberData(nameof(ReturnsPocoAdapterData))]
        public void Visit_ValidPath_ReturnsExpandoAdapter(object targetObject, string path, object expectedTargetObject)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath(path), new DefaultContractResolver());
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Same(expectedTargetObject, targetObject);
            Assert.IsType<PocoAdapter>(adapter);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public void Visit_InvalidIndexToArray_Fails(string position)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath($"/Customers/{position}/States/-"), new DefaultContractResolver());
            var automobileDepartment = new Class1Nested();
            object targetObject = automobileDepartment;
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.False(visitStatus);
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", position),
                message);
        }

        [Theory]
        [InlineData("-")]
        [InlineData("foo")]
        public void Visit_InvalidIndexFormatToArray_Fails(string position)
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath($"/Customers/{position}/States/-"), new DefaultContractResolver());
            var automobileDepartment = new Class1Nested();
            object targetObject = automobileDepartment;
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.False(visitStatus);
            Assert.Equal(string.Format(
                "The path segment '{0}' is invalid for an array index.", position),
                message);
        }

        // The adapter takes care of the responsibility of validating the final segment
        [Fact]
        public void Visit_DoesNotValidate_FinalPathSegment()
        {
            // Arrange
            var visitor = new ObjectVisitor(new ParsedPath($"/NonExisting"), new DefaultContractResolver());
            var model = new Class1();
            object targetObject = model;
            IAdapter adapter = null;
            string message = null;

            // Act
            var visitStatus = visitor.TryVisit(ref targetObject, out adapter, out message);

            // Assert
            Assert.True(visitStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.IsType<PocoAdapter>(adapter);
        }
    }
}

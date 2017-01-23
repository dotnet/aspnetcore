// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using Moq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    public class ListAdapterTest
    {
        [Fact]
        public void Patch_OnArrayObject_Fails()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new[] { 20, 30 };
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, "0", resolver.Object, "40", out message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(
                string.Format(
                    "The type '{0}' which is an array is not supported for json patch operations as it has a fixed size.",
                    targetObject.GetType().FullName),
                message);
        }

        [Fact]
        public void Patch_OnNonGenericListObject_Fails()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new ArrayList();
            targetObject.Add(20);
            targetObject.Add(30);
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, "-", resolver.Object, "40", out message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(
                string.Format(
                    "The type '{0}' which is a non generic list is not supported for json patch operations. Only generic list types are supported.",
                    targetObject.GetType().FullName),
                message);
        }

        [Fact]
        public void Add_WithIndexSameAsNumberOfElements_Works()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<string>() { "James", "Mike" };
            var listAdapter = new ListAdapter();
            string message = null;
            var position = targetObject.Count.ToString();

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, position, resolver.Object, "Rob", out message);

            // Assert
            Assert.Null(message);
            Assert.True(addStatus);
            Assert.Equal(3, targetObject.Count);
            Assert.Equal(new List<string>() { "James", "Mike", "Rob" }, targetObject);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("-2")]
        [InlineData("3")]
        public void Add_WithOutOfBoundsIndex_Fails(string position)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<string>() { "James", "Mike" };
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, position, resolver.Object, "40", out message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", position),
                message);
        }

        [Theory]
        [InlineData("_")]
        [InlineData("blah")]
        public void Patch_WithInvalidPositionFormat_Fails(string position)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<string>() { "James", "Mike" };
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, position, resolver.Object, "40", out message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(
                string.Format("The path segment '{0}' is invalid for an array index.", position),
                message);
        }

        public static TheoryData<List<int>, List<int>> AppendAtEndOfListData
        {
            get
            {
                return new TheoryData<List<int>, List<int>>()
                {
                    {
                        new List<int>() {  },
                        new List<int>() { 20 }
                    },
                    {
                        new List<int>() { 5, 10 },
                        new List<int>() { 5, 10, 20 }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AppendAtEndOfListData))]
        public void Add_Appends_AtTheEnd(List<int> targetObject, List<int> expected)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, "-", resolver.Object, "20", out message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected.Count, targetObject.Count);
            Assert.Equal(expected, targetObject);
        }

        [Fact]
        public void Add_NullObject_ToReferenceTypeListWorks()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var listAdapter = new ListAdapter();
            var targetObject = new List<string>() { "James", "Mike" };
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, "-", resolver.Object, value: null, errorMessage: out message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(3, targetObject.Count);
            Assert.Equal(new List<string>() { "James", "Mike", null }, targetObject);
        }

        [Fact]
        public void Add_NonCompatibleType_Fails()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = (new List<int>() { 10, 20 }).AsReadOnly();
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, "-", resolver.Object, "James", out message);

            // Assert
            Assert.False(addStatus);
            Assert.Equal(string.Format("The value '{0}' is invalid for target location.", "James"), message);
        }

        public static TheoryData<IList, object, string, IList> AddingDifferentComplexTypeWorksData
        {
            get
            {
                return new TheoryData<IList, object, string, IList>()
                {
                    {
                        new List<string>() { },
                        "a",
                        "-",
                        new List<string>() { "a" }
                    },
                    {
                        new List<string>() { "a", "b" },
                        "c",
                        "-",
                        new List<string>() { "a", "b", "c" }
                    },
                    {
                        new List<string>() { "a", "b" },
                        "c",
                        "0",
                        new List<string>() { "c", "a", "b" }
                    },
                    {
                        new List<string>() { "a", "b" },
                        "c",
                        "1",
                        new List<string>() { "a", "c", "b" }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddingDifferentComplexTypeWorksData))]
        public void Add_DifferentComplexTypeWorks(IList targetObject, object value, string position, IList expected)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var addStatus = listAdapter.TryAdd(targetObject, position, resolver.Object, value, out message);

            // Assert
            Assert.True(addStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected.Count, targetObject.Count);
            Assert.Equal(expected, targetObject);
        }

        [Theory]
        [InlineData(new int[] { }, "0")]
        [InlineData(new[] { 10, 20 }, "-1")]
        [InlineData(new[] { 10, 20 }, "2")]
        public void Get_IndexOutOfBounds(int[] input, string position)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            string message = null;
            object value = null;

            // Act
            var getStatus = listAdapter.TryGet(targetObject, position, resolver.Object, out value, out message);

            // Assert
            Assert.False(getStatus);
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", position),
                message);
        }

        [Theory]
        [InlineData(new[] { 10, 20 }, "0", 10)]
        [InlineData(new[] { 10, 20 }, "1", 20)]
        [InlineData(new[] { 10 }, "0", 10)]
        public void Get(int[] input, string position, object expected)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            string message = null;
            object value = null;

            // Act
            var getStatus = listAdapter.TryGet(targetObject, position, resolver.Object, out value, out message);

            // Assert
            Assert.True(getStatus);
            Assert.Equal(expected, value);
            Assert.Equal(new List<int>(input), targetObject);
        }

        [Theory]
        [InlineData(new int[] { }, "0")]
        [InlineData(new[] { 10, 20 }, "-1")]
        [InlineData(new[] { 10, 20 }, "2")]
        public void Remove_IndexOutOfBounds(int[] input, string position)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var removeStatus = listAdapter.TryRemove(targetObject, position, resolver.Object, out message);

            // Assert
            Assert.False(removeStatus);
            Assert.Equal(
                string.Format("The index value provided by path segment '{0}' is out of bounds of the array size.", position),
                message);
        }

        [Theory]
        [InlineData(new[] { 10, 20 }, "0", new[] { 20 })]
        [InlineData(new[] { 10, 20 }, "1", new[] { 10 })]
        [InlineData(new[] { 10 }, "0", new int[] { })]
        public void Remove(int[] input, string position, int[] expected)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>(input);
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var removeStatus = listAdapter.TryRemove(targetObject, position, resolver.Object, out message);

            // Assert
            Assert.True(removeStatus);
            Assert.Equal(new List<int>(expected), targetObject);
        }

        [Fact]
        public void Replace_NonCompatibleType_Fails()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = (new List<int>() { 10, 20 }).AsReadOnly();
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, "-", resolver.Object, "James", out message);

            // Assert
            Assert.False(replaceStatus);
            Assert.Equal(
                string.Format("The value '{0}' is invalid for target location.", "James"),
                message);
        }

        [Fact]
        public void Replace_ReplacesValue_AtTheEnd()
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>() { 10, 20 };
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, "-", resolver.Object, "30", out message);

            // Assert
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(new List<int>() { 10, 30 }, targetObject);
        }

        public static TheoryData<string, List<int>> ReplacesValuesAtPositionData
        {
            get
            {
                return new TheoryData<string, List<int>>()
                {
                    {
                        "0",
                        new List<int>() { 30, 20 }
                    },
                    {
                        "1",
                        new List<int>() { 10, 30 }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(ReplacesValuesAtPositionData))]
        public void Replace_ReplacesValue_AtGivenPosition(string position, List<int> expected)
        {
            // Arrange
            var resolver = new Mock<IContractResolver>(MockBehavior.Strict);
            var targetObject = new List<int>() { 10, 20 };
            var listAdapter = new ListAdapter();
            string message = null;

            // Act
            var replaceStatus = listAdapter.TryReplace(targetObject, position, resolver.Object, "30", out message);

            // Assert
            Assert.True(replaceStatus);
            Assert.True(string.IsNullOrEmpty(message), "Expected no error message");
            Assert.Equal(expected, targetObject);
        }
    }
}

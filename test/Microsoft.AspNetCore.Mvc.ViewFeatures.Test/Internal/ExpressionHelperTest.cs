// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ExpressionHelperTest
    {
        private readonly ExpressionTextCache _expressionTextCache = new ExpressionTextCache();

        public static IEnumerable<object[]> ExpressionAndTexts
        {
            get
            {
                var i = 3;
                var value = "Test";
                var Model = new TestModel();
                var key = "TestModel";
                var myModels = new List<TestModel>();

                return new TheoryData<Expression, string>
                {
                    {
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory),
                        "SelectedCategory"
                    },
                    {
                        (Expression<Func<TestModel, Category>>)(m => Model.SelectedCategory),
                        "SelectedCategory"
                    },
                    {
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName),
                        "SelectedCategory.CategoryName"
                    },
                    {
                        (Expression<Func<TestModel, int>>)(testModel => testModel.SelectedCategory.CategoryId),
                        "SelectedCategory.CategoryId"
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.SelectedCategory.CategoryName.MainCategory),
                        "SelectedCategory.CategoryName.MainCategory"
                    },
                    {
                        (Expression<Func<TestModel, TestModel>>)(model => model),
                        string.Empty
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => value),
                        "value"
                    },
                    {
                        (Expression<Func<TestModel, TestModel>>)(m => Model),
                        string.Empty
                    },
                    {
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory),
                        "[2].SelectedCategory"
                    },
                    {
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[i].SelectedCategory),
                        "[3].SelectedCategory"
                    },
                    {
                        (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory),
                        "[TestModel].SelectedCategory.CategoryName.MainCategory"
                    },
                    {
                        (Expression<Func<TestModel, int>>)(model => model.PreferredCategories[i].CategoryId),
                        "PreferredCategories[3].CategoryId"
                    },
                    {
                        (Expression<Func<IList<TestModel>, Category>>)(model => myModels[i].SelectedCategory),
                        "myModels[3].SelectedCategory"
                    },
                    {
                        (Expression<Func<IList<TestModel>, int>>)(model => model[2].PreferredCategories[i].CategoryId),
                        "[2].PreferredCategories[3].CategoryId"
                    },
                };
            }
        }

        public static IEnumerable<object[]> CachedExpressions
        {
            get
            {
                var key = "TestModel";
                var myModel = new TestModel();

                return new TheoryData<Expression>
                {
                    (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory),
                    (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName),
                    (Expression<Func<TestModel, int>>)(testModel => testModel.SelectedCategory.CategoryId),
                    (Expression<Func<TestModel, string>>)(model => model.SelectedCategory.CategoryName.MainCategory),
                    (Expression<Func<TestModel, string>>)(testModel => key),
                    (Expression<Func<TestModel, TestModel>>)(m => m),
                    (Expression<Func<TestModel, Category>>)(m => myModel.SelectedCategory),
                };
            }
        }

        public static IEnumerable<object[]> IndexerExpressions
        {
            get
            {
                var i = 3;
                var key = "TestModel";
                var myModels = new List<TestModel>();

                return new TheoryData<Expression>
                {
                    (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory),
                    (Expression<Func<IList<TestModel>, Category>>)(model => myModels[i].SelectedCategory),
                    (Expression<Func<IList<TestModel>, CategoryName>>)(testModel => testModel[i].SelectedCategory.CategoryName),
                    (Expression<Func<TestModel, int>>)(model => model.PreferredCategories[i].CategoryId),
                    (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory),
                };
            }
        }

        public static IEnumerable<object[]> EquivalentExpressions
        {
            get
            {
                var value = "Test";
                var Model = "Test";

                return new TheoryData<Expression, Expression>
                {
                    {
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory),
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory)
                    },
                    {
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName),
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName)
                    },
                    {
                        (Expression<Func<TestModel, int>>)(testModel => testModel.SelectedCategory.CategoryId),
                        (Expression<Func<TestModel, int>>)(testModel => testModel.SelectedCategory.CategoryId)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.SelectedCategory.CategoryName.MainCategory),
                        (Expression<Func<TestModel, string>>)(model => model.SelectedCategory.CategoryName.MainCategory)
                    },
                    {
                        (Expression<Func<TestModel, TestModel>>)(model => model),
                        (Expression<Func<TestModel, TestModel>>)(m => m)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => value),
                        (Expression<Func<TestModel, string>>)(m => value)
                    },
                    {
                        // These two expressions are not actually equivalent. However ExpressionHelper returns 
                        // string.Empty for these two expressions and hence they are considered as equivalent by the 
                        // cache.
                        (Expression<Func<TestModel, string>>)(m => Model),
                        (Expression<Func<TestModel, TestModel>>)(m => m)
                    },
                };
            }
        }

        public static IEnumerable<object[]> NonEquivalentExpressions
        {
            get
            {
                var value = "test";
                var key = "TestModel";
                var Model = "Test";
                var myModel = new TestModel();

                return new TheoryData<Expression, Expression>
                {
                    {
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory),
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.Model),
                        (Expression<Func<TestModel, string>>)(model => model.Name)
                    },
                    {
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName),
                        (Expression<Func<TestModel, string>>)(model => value)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(testModel => testModel.SelectedCategory.CategoryName.MainCategory),
                        (Expression<Func<TestModel, string>>)(testModel => value)
                    },
                    {
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory),
                        (Expression<Func<TestModel, string>>)(model => model.SelectedCategory.CategoryName.MainCategory)
                    },
                    {
                        (Expression<Func<TestModel, int>>)(testModel => testModel.SelectedCategory.CategoryId),
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory)
                    },
                    {
                        (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory),
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory)
                    },
                    {
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory),
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => Model),
                        (Expression<Func<TestModel, string>>)(m => m.Model)
                    },
                    {
                        (Expression<Func<TestModel, TestModel>>)(m => m),
                        (Expression<Func<TestModel, string>>)(m => m.Model)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => myModel.Name),
                        (Expression<Func<TestModel, string>>)(m => m.Name)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => key),
                        (Expression<Func<TestModel, string>>)(m => value)
                    },
                    {
                        (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory),
                        (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory)
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ExpressionAndTexts))]
        public void GetExpressionText_ReturnsExpectedExpressionText(LambdaExpression expression, string expressionText)
        {
            // Act
            var text = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Assert
            Assert.Equal(expressionText, text);
        }

        [Theory]
        [MemberData(nameof(CachedExpressions))]
        public void GetExpressionText_CachesExpression(LambdaExpression expression)
        {
            // Act - 1
            var text1 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Act - 2
            var text2 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Assert
            Assert.Same(text1, text2); // Cached
        }

        [Theory]
        [MemberData(nameof(IndexerExpressions))]
        public void GetExpressionText_DoesNotCacheIndexerExpression(LambdaExpression expression)
        {
            // Act - 1
            var text1 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Act - 2
            var text2 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Assert
            Assert.NotSame(text1, text2); // not cached
        }

        [Theory]
        [MemberData(nameof(EquivalentExpressions))]
        public void GetExpressionText_CacheEquivalentExpressions(LambdaExpression expression1, LambdaExpression expression2)
        {
            // Act - 1
            var text1 = ExpressionHelper.GetExpressionText(expression1, _expressionTextCache);

            // Act - 2
            var text2 = ExpressionHelper.GetExpressionText(expression2, _expressionTextCache);

            // Assert
            Assert.Same(text1, text2);
        }

        [Theory]
        [MemberData(nameof(NonEquivalentExpressions))]
        public void GetExpressionText_CheckNonEquivalentExpressions(LambdaExpression expression1, LambdaExpression expression2)
        {
            // Act - 1
            var text1 = ExpressionHelper.GetExpressionText(expression1, _expressionTextCache);

            // Act - 2
            var text2 = ExpressionHelper.GetExpressionText(expression2, _expressionTextCache);

            // Assert
            Assert.NotSame(text1, text2);
        }

        private class TestModel
        {
            public string Name { get; set; }
            public string Model { get; set; }
            public Category SelectedCategory { get; set; }

            public IList<Category> PreferredCategories { get; set; }
        }

        private class Category
        {
            public int CategoryId { get; set; }
            public CategoryName CategoryName { get; set; }
        }

        private class CategoryName
        {
            public string MainCategory { get; set; }
            public string SubCategory { get; set; }
        }
    }
}

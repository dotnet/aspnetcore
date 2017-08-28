// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ExpressionHelperTest
    {
        private readonly ExpressionTextCache _expressionTextCache = new ExpressionTextCache();

        public static TheoryData<Expression, string> ExpressionAndTexts
        {
            get
            {
                var i = 3;
                var value = "Test";
                var key = "TestModel";
                var myModels = new List<TestModel>();
                var models = new List<TestModel>();
                var modelTest = new TestModel();
                var modelType = typeof(TestModel);

                var data = new TheoryData<Expression, string>
                {
                    {
                        (Expression<Func<TestModel, Category>>)(model => model.SelectedCategory),
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
                        (Expression<Func<LowerModel, int>>)(testModel => testModel.selectedcategory.CategoryId),
                        "selectedcategory.CategoryId"
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
                        (Expression<Func<TestModel, int>>)(model => models[0].SelectedCategory.CategoryId),
                        "models[0].SelectedCategory.CategoryId"
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => modelTest.Name),
                        "modelTest.Name"
                    },
                    {
                        (Expression<Func<TestModel, Type>>)(model => modelType),
                        "modelType"
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
                        (Expression<Func<IList<LowerModel>, Category>>)(model => model[i].selectedcategory),
                        "[3].selectedcategory"
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
                    {
                        (Expression<Func<IList<LowerModel>, int>>)(model => model[2].preferredcategories[i].CategoryId),
                        "[2].preferredcategories[3].CategoryId"
                    },
                    {
                        (Expression<Func<IList<TestModel>, string>>)(model => model.FirstOrDefault().Name),
                        "Name"
                    },
                    {
                        (Expression<Func<IList<LowerModel>, string>>)(model => model.FirstOrDefault().name),
                        "name"
                    },
                    {
                        (Expression<Func<IList<TestModel>, string>>)(model => model.FirstOrDefault().Model),
                        "Model"
                    },
                    {
                        (Expression<Func<IList<TestModel>, int>>)(model => model.FirstOrDefault().SelectedCategory.CategoryId),
                        "SelectedCategory.CategoryId"
                    },
                    {
                        (Expression<Func<IList<TestModel>, string>>)(model => model.FirstOrDefault().SelectedCategory.CategoryName.MainCategory),
                        "SelectedCategory.CategoryName.MainCategory"
                    },
                    {
                        (Expression<Func<IList<TestModel>, int>>)(model => model.FirstOrDefault().PreferredCategories.Count),
                        "PreferredCategories.Count"
                    },
                    {
                        (Expression<Func<IList<TestModel>, int>>)(model => model.FirstOrDefault().PreferredCategories.FirstOrDefault().CategoryId),
                        "CategoryId"
                    },
                    // Constants are not supported.
                    {
                        // Namespace never appears in expression name. "Model" there doesn't matter.
                        (Expression<Func<TestModel, int>>)(m => Microsoft.AspNetCore.Mvc.ViewFeatures.Model.Constants.WoodstockYear),
                        string.Empty
                    },
                    {
                        // Class name never appears in expression name. "Model" there doesn't matter.
                        (Expression<Func<TestModel, int>>)(m => Model.Constants.WoodstockYear),
                        string.Empty
                    },
                    // ExpressionHelper treats static properties like other member accesses. Similarly to
                    // RazorPage.Model, name "Model" is ignored at LHS of these expressions. This is a rare case because
                    // static properties are the only leftmost member accesses that can reach beyond the current class.
                    {
                        (Expression<Func<TestModel, string>>)(m => Model.Constants.Model.Name),
                        "Name"
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => AStaticClass.Model),
                        string.Empty
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => AStaticClass.Test),
                        "Test"
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => AnotherStaticClass.Model.Name),
                        "Name"
                    },
                    {
                        (Expression<Func<TestModel, string>>)(m => AnotherStaticClass.Test.Name),
                        "Test.Name"
                    },
                };

                {
                    // Nearly impossible in a .cshtml file because model is a keyword.
                    var model = "Some string";
                    data.Add((Expression<Func<TestModel, string>>)(m => model), string.Empty);
                }

                {
                    // Model property in RazorPage is "special" (in a good way).
                    var Model = new TestModel();
                    data.Add((Expression<Func<TestModel, TestModel>>)(m => Model), string.Empty);
                    data.Add((Expression<Func<TestModel, TestModel>>)(model => Model), string.Empty);
                    data.Add((Expression<Func<TestModel, Category>>)(m => Model.SelectedCategory), "SelectedCategory");
                }

                return data;
            }
        }

        public static TheoryData<Expression> CachedExpressions
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

        public static TheoryData<Expression> IndexerExpressions
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

        public static TheoryData<Expression> UnsupportedExpressions
        {
            get
            {
                var i = 2;
                var j = 3;

                return new TheoryData<Expression>
                {
                    // Indexers that have multiple arguments.
                    (Expression<Func<TestModel[][], string>>)(model => model[23][3].Name),
                    (Expression<Func<TestModel[][], string>>)(model => model[i][3].Name),
                    (Expression<Func<TestModel[][], string>>)(model => model[23][j].Name),
                    (Expression<Func<TestModel[][], string>>)(model => model[i][j].Name),
                    // Calls that aren't indexers.
                    (Expression<Func<IList<TestModel>, string>>)(model => model.FirstOrDefault().Name),
                    (Expression<Func<IList<TestModel>, string>>)(model => model.FirstOrDefault().SelectedCategory.CategoryName.MainCategory),
                    (Expression<Func<IList<TestModel>, int>>)(model => model.FirstOrDefault().PreferredCategories.FirstOrDefault().CategoryId),
                };
            }
        }

        public static TheoryData<Expression, Expression> EquivalentExpressions
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

        public static TheoryData<Expression, Expression> NonEquivalentExpressions
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
                        (Expression<Func<TestModel, CategoryName>>)(model => model.SelectedCategory.CategoryName),
                        (Expression<Func<LowerModel, CategoryName>>)(model => model.selectedcategory.CategoryName)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.Model),
                        (Expression<Func<TestModel, string>>)(model => model.Name)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.Model),
                        (Expression<Func<LowerModel, string>>)(model => model.model)
                    },
                    {
                        (Expression<Func<TestModel, string>>)(model => model.Name),
                        (Expression<Func<LowerModel, string>>)(model => model.name)
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
                        (Expression<Func<IList<TestModel>, Category>>)(model => model[2].SelectedCategory),
                        (Expression<Func<IList<LowerModel>, Category>>)(model => model[2].selectedcategory)
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
                        (Expression<Func<IDictionary<string, TestModel>, string>>)(model => model[key].SelectedCategory.CategoryName.MainCategory),
                        (Expression<Func<IDictionary<string, LowerModel>, string>>)(model => model[key].selectedcategory.CategoryName.MainCategory)
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
            Assert.Same(text1, text2); // cached
        }

        [Theory]
        [MemberData(nameof(IndexerExpressions))]
        [MemberData(nameof(UnsupportedExpressions))]
        public void GetExpressionText_DoesNotCacheIndexerOrUnspportedExpression(LambdaExpression expression)
        {
            // Act - 1
            var text1 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Act - 2
            var text2 = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);

            // Assert
            Assert.Equal(text1, text2, StringComparer.Ordinal);
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
            Assert.Same(text1, text2); // cached
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
            Assert.NotEqual(text1, text2, StringComparer.Ordinal);
            Assert.NotSame(text1, text2);
        }

        private class TestModel
        {
            public string Name { get; set; }
            public string Model { get; set; }
            public Category SelectedCategory { get; set; }

            public IList<Category> PreferredCategories { get; set; }
        }

        private class LowerModel
        {
            public string name { get; set; }

            public string model { get; set; }

            public Category selectedcategory { get; set; }

            public IList<Category> preferredcategories { get; set; }
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

        private static class AStaticClass
        {
            public static string Model { get; set; }
            public static string Test { get; set; }
        }

        private static class AnotherStaticClass
        {
            public static Model.Model Model { get; set; }
            public static Model.Model Test { get; set; }
        }
    }
}

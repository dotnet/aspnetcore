// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class PageConventionCollectionExtensionsTest
    {
        [Fact]
        public void AddFilter_AddsFiltersToAllPages()
        {
            // Arrange
            var filter = Mock.Of<IFilterMetadata>();
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.ConfigureFilter(filter);
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Same(filter, Assert.Single(model.Filters)),
                model => Assert.Same(filter, Assert.Single(model.Filters)),
                model => Assert.Same(filter, Assert.Single(model.Filters)));
        }

        [Fact]
        public void AuthorizePage_AddsAllowAnonymousFilterToSpecificPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizeFolder("/Users");
            conventions.AllowAnonymousToPage("/Users/Contact");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                },
                model =>
                {
                    Assert.Equal("/Users/Contact", model.ViewEnginePath);
                    Assert.IsType<AuthorizeFilter>(model.Filters[0]);
                    Assert.IsType<AllowAnonymousFilter>(model.Filters[1]);
                });
        }

        [Fact]
        public void AllowAnonymousToAreaPage_AddsAllowAnonymousFilterToSpecificPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Profile.cshtml", "/Profile", "Accounts"),
            };

            // Act
            conventions.AllowAnonymousToAreaPage("Accounts", "/Profile");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Profile.cshtml", model.RelativePath);
                    Assert.IsType<AllowAnonymousFilter>(Assert.Single(model.Filters));
                });
        }

        [Theory]
        [InlineData("/Users")]
        [InlineData("/Users/")]
        public void AuthorizePage_AddsAllowAnonymousFilterToPagesUnderFolder(string folderName)
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizeFolder("/");
            conventions.AllowAnonymousToFolder("/Users");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model =>
                {
                    Assert.Equal("/Index", model.ViewEnginePath);
                    Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                },
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    Assert.IsType<AuthorizeFilter>(model.Filters[0]);
                    Assert.IsType<AllowAnonymousFilter>(model.Filters[1]);
                },
                model =>
                {
                    Assert.Equal("/Users/Contact", model.ViewEnginePath);
                    Assert.IsType<AuthorizeFilter>(model.Filters[0]);
                    Assert.IsType<AllowAnonymousFilter>(model.Filters[1]);
                });
        }

        [Fact]
        public void AllowAnonymousToAreaFolder_AddsAllowAnonymousFilterToFolderInArea()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Mange/Profile.cshtml", "/Manage/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/Profile.cshtml", "/Manage/Profile", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/2FA.cshtml", "/Manage/2FA", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/View/OrderHistory.cshtml", "/View/OrderHistory", "Accounts"),
            };

            // Act
            conventions.AllowAnonymousToAreaFolder("Accounts", "/Manage");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/Profile.cshtml", model.RelativePath);
                    Assert.IsType<AllowAnonymousFilter>(Assert.Single(model.Filters));
                },
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/2FA.cshtml", model.RelativePath);
                    Assert.IsType<AllowAnonymousFilter>(Assert.Single(model.Filters));
                },
                model => Assert.Empty(model.Filters));
        }

        [Fact]
        public void AuthorizePage_AddsAuthorizeFilterWithPolicyToSpecificPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizePage("/Users/Account", "Manage-Accounts");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                },
                model => Assert.Empty(model.Filters));
        }

        [Fact]
        public void AuthorizeAreaPage_AddsAuthorizeFilterWithDefaultPolicyToAreaPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Profile.cshtml", "/Profile", "Accounts"),
            };

            // Act
            conventions.AuthorizeAreaPage("Accounts", "/Profile");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Profile.cshtml", model.RelativePath);
                    var authFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeAttribute = Assert.Single(authFilter.AuthorizeData);
                    Assert.Empty(authorizeAttribute.Policy);
                });
        }

        [Fact]
        public void AuthorizeAreaPage_AddsAuthorizeFilterWithCustomPolicyToAreaPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Profile.cshtml", "/Profile", "Accounts"),
            };

            // Act
            conventions.AuthorizeAreaPage("Accounts", "/Profile", "custom");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Profile.cshtml", model.RelativePath);
                    var authFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeAttribute = Assert.Single(authFilter.AuthorizeData);
                    Assert.Equal("custom", authorizeAttribute.Policy);
                });
        }

        [Fact]
        public void AuthorizePage_AddsAuthorizeFilterWithoutPolicyToSpecificPage()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizePage("/Users/Account");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal(string.Empty, authorizeData.Policy);
                },
                model => Assert.Empty(model.Filters));
        }

        [Theory]
        [InlineData("/Users")]
        [InlineData("/Users/")]
        public void AuthorizePage_AddsAuthorizeFilterWithPolicyToPagesUnderFolder(string folderName)
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizeFolder(folderName, "Manage-Accounts");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                },
                model =>
                {
                    Assert.Equal("/Users/Contact", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("Manage-Accounts", authorizeData.Policy);
                });
        }

        [Theory]
        [InlineData("/Users")]
        [InlineData("/Users/")]
        public void AuthorizePage_AddsAuthorizeFilterWithoutPolicyToPagesUnderFolder(string folderName)
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Pages/Index.cshtml", "/Index.cshtml"),
                CreateApplicationModel("/Pages/Users/Account.cshtml", "/Users/Account"),
                CreateApplicationModel("/Pages/Users/Contact.cshtml", "/Users/Contact"),
            };

            // Act
            conventions.AuthorizeFolder(folderName);
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Users/Account", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal(string.Empty, authorizeData.Policy);
                },
                model =>
                {
                    Assert.Equal("/Users/Contact", model.ViewEnginePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal(string.Empty, authorizeData.Policy);
                });
        }

        [Fact]
        public void AuthorizeAreaFolder_AddsAuthorizeFilterWithDefaultPolicyToAreaPagesInFolder()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Mange/Profile.cshtml", "/Manage/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/Profile.cshtml", "/Manage/Profile", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/2FA.cshtml", "/Manage/2FA", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/View/OrderHistory.cshtml", "/View/OrderHistory", "Accounts"),
            };

            // Act
            conventions.AuthorizeAreaFolder("Accounts", "/Manage");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/Profile.cshtml", model.RelativePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Empty(authorizeData.Policy);
                },
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/2FA.cshtml", model.RelativePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Empty(authorizeData.Policy);
                },
                model => Assert.Empty(model.Filters));
        }

        [Fact]
        public void AuthorizeAreaFolder_AddsAuthorizeFilterWithCustomPolicyToAreaPagesInFolder()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                CreateApplicationModel("/Profile.cshtml", "/Profile"),
                CreateApplicationModel("/Mange/Profile.cshtml", "/Manage/Profile"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/Profile.cshtml", "/Manage/Profile", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/Manage/2FA.cshtml", "/Manage/2FA", "Accounts"),
                CreateApplicationModel("/Areas/Accounts/Pages/View/OrderHistory.cshtml", "/View/OrderHistory", "Accounts"),
            };

            // Act
            conventions.AuthorizeAreaFolder("Accounts", "/Manage", "custom");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model => Assert.Empty(model.Filters),
                model => Assert.Empty(model.Filters),
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/Profile.cshtml", model.RelativePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("custom", authorizeData.Policy);
                },
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Manage/2FA.cshtml", model.RelativePath);
                    var authorizeFilter = Assert.IsType<AuthorizeFilter>(Assert.Single(model.Filters));
                    var authorizeData = Assert.IsType<AuthorizeAttribute>(Assert.Single(authorizeFilter.AuthorizeData));
                    Assert.Equal("custom", authorizeData.Policy);
                },
                model => Assert.Empty(model.Filters));
        }

        [Fact]
        public void AddPageRoute_AddsRouteToSelector()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                new PageRouteModel("/Pages/Index.cshtml", "/Index")
                {
                    Selectors =
                    {
                        CreateSelectorModel("Index", suppressLinkGeneration: true),
                        CreateSelectorModel(""),
                    }
                },
                new PageRouteModel("/Pages/About.cshtml", "/About")
                {
                    Selectors =
                    {
                        CreateSelectorModel("About"),
                    }
                }
            };

            // Act
            conventions.AddPageRoute("/Index", "Different-Route");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model =>
                {
                    Assert.Equal("/Index", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector =>
                        {
                            Assert.Equal("Index", selector.AttributeRouteModel.Template);
                            Assert.True(selector.AttributeRouteModel.SuppressLinkGeneration);
                        },
                        selector =>
                        {
                            Assert.Equal("", selector.AttributeRouteModel.Template);
                            Assert.True(selector.AttributeRouteModel.SuppressLinkGeneration);
                        },
                        selector =>
                        {
                            Assert.Equal("Different-Route", selector.AttributeRouteModel.Template);
                            Assert.False(selector.AttributeRouteModel.SuppressLinkGeneration);
                        });
                },
                model =>
                {
                    Assert.Equal("/About", model.ViewEnginePath);
                    Assert.Collection(model.Selectors,
                        selector =>
                        {
                            Assert.Equal("About", selector.AttributeRouteModel.Template);
                            Assert.False(selector.AttributeRouteModel.SuppressLinkGeneration);
                        });
                });
        }

        [Fact]
        public void AddAreaPageRoute_AddsRouteToSelector()
        {
            // Arrange
            var conventions = new PageConventionCollection();
            var models = new[]
            {
                new PageRouteModel("/Pages/Profile.cshtml", "/Profile")
                {
                    Selectors =
                    {
                        CreateSelectorModel("Profile"),
                    }
                },
                new PageRouteModel("/Areas/Accounts/Pages/Profile.cshtml", "/Profile", "Accounts")
                {
                    Selectors =
                    {
                        CreateSelectorModel("Accounts/Profile"),
                    }
                }
            };

            // Act
            conventions.AddAreaPageRoute("Accounts", "/Profile", "Different-Route");
            ApplyConventions(conventions, models);

            // Assert
            Assert.Collection(models,
                model =>
                {
                    Assert.Equal("/Pages/Profile.cshtml", model.RelativePath);
                    Assert.Collection(model.Selectors,
                        selector =>
                        {
                            Assert.Equal("Profile", selector.AttributeRouteModel.Template);
                            Assert.False(selector.AttributeRouteModel.SuppressLinkGeneration);
                        });
                },
                model =>
                {
                    Assert.Equal("/Areas/Accounts/Pages/Profile.cshtml", model.RelativePath);
                    Assert.Collection(model.Selectors,
                        selector =>
                        {
                            Assert.Equal("Accounts/Profile", selector.AttributeRouteModel.Template);
                            Assert.True (selector.AttributeRouteModel.SuppressLinkGeneration);
                        },
                        selector =>
                        {
                            Assert.Equal("Different-Route", selector.AttributeRouteModel.Template);
                            Assert.False(selector.AttributeRouteModel.SuppressLinkGeneration);
                        });
                });
        }

        private static SelectorModel CreateSelectorModel(string template, bool suppressLinkGeneration = false)
        {
            return new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel
                {
                    Template = template,
                    SuppressLinkGeneration = suppressLinkGeneration
                },
            };
        }

        private static void ApplyConventions(PageConventionCollection conventions, PageRouteModel[] models)
        {
            foreach (var convention in conventions.OfType<IPageRouteModelConvention>())
            {
                foreach (var model in models)
                {
                    convention.Apply(model);
                }
            }
        }
        private static void ApplyConventions(PageConventionCollection conventions, PageApplicationModel[] models)
        {
            foreach (var convention in conventions.OfType<IPageApplicationModelConvention>())
            {
                foreach (var model in models)
                {
                    convention.Apply(model);
                }
            }
        }

        private PageApplicationModel CreateApplicationModel(string relativePath, string viewEnginePath, string areaName = null)
        {
            var descriptor = new PageActionDescriptor
            {
                ViewEnginePath = viewEnginePath,
                RelativePath = relativePath,
                AreaName = areaName,
            };

            return new PageApplicationModel(descriptor, typeof(object).GetTypeInfo(), new object[0]);
        }
    }
}

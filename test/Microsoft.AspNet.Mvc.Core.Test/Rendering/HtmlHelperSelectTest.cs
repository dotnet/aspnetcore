// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperSelectTest
    {
        private static readonly SelectListGroup GroupOne = new SelectListGroup { Name = "Group One", };
        private static readonly SelectListGroup GroupTwo = new SelectListGroup { Name = "Group Two", };
        private static readonly SelectListGroup DisabledGroup = new SelectListGroup
        {
            Disabled = true,
            Name = "Disabled Group",
        };

        private static readonly List<SelectListItem> BasicSelectList = new List<SelectListItem>
        {
            new SelectListItem { Text = "Zero",  Value = "0"},
            new SelectListItem { Text = "One",   Value = "1"},
            new SelectListItem { Text = "Two",   Value = "2"},
            new SelectListItem { Text = "Three", Value = "3"},
        };
        private static readonly List<SelectListItem> SomeDisabledOneSelectedSelectList = new List<SelectListItem>
        {
            new SelectListItem { Disabled = false, Selected = false, Text = "Zero",  Value = "0"},
            new SelectListItem { Disabled = true,  Selected = true,  Text = "One",   Value = "1"},
            new SelectListItem { Disabled = false, Selected = false, Text = "Two",  Value = "2"},
            new SelectListItem { Disabled = true,  Selected = false, Text = "Three", Value = "3"},
        };
        private static readonly List<SelectListItem> SomeGroupedSomeSelectedSelectList = new List<SelectListItem>
        {
            new SelectListItem { Group = GroupOne, Selected = true,  Text = "Zero",  Value = "0"},
            new SelectListItem { Group = GroupTwo, Selected = false, Text = "One",   Value = "1"},
            new SelectListItem { Group = GroupOne, Selected = true,  Text = "Two",   Value = "2"},
            new SelectListItem { Group = null,     Selected = false, Text = "Three", Value = "3"},
        };
        private static readonly List<SelectListItem> OneGroupSomeSelectedSelectList = new List<SelectListItem>
        {
            new SelectListItem { Group = GroupOne, Selected = true,  Text = "Zero",  Value = "0"},
            new SelectListItem { Group = GroupOne, Selected = true,  Text = "One",   Value = "1"},
            new SelectListItem { Group = GroupOne, Selected = false, Text = "Two",   Value = "2"},
            new SelectListItem { Group = GroupOne, Selected = false, Text = "Three", Value = "3"},
        };
        private static readonly List<SelectListItem> OneDisabledGroupAllSelectedSelectList = new List<SelectListItem>
        {
            new SelectListItem { Group = DisabledGroup, Selected = true, Text = "Zero",  Value = "0"},
            new SelectListItem { Group = DisabledGroup, Selected = true, Text = "One",   Value = "1"},
            new SelectListItem { Group = DisabledGroup, Selected = true, Text = "Two",   Value = "2"},
            new SelectListItem { Group = DisabledGroup, Selected = true, Text = "Three", Value = "3"},
        };

        // Select list -> expected HTML with null model, expected HTML with model containing "2".
        public static TheoryData<IEnumerable<SelectListItem>, string, string> DropDownListDataSet
        {
            get
            {
                return new TheoryData<IEnumerable<SelectListItem>, string, string>
                {
                    {
                        BasicSelectList,
                        "<select id=\"Property1\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        SomeDisabledOneSelectedSelectList,
                        "<select id=\"Property1\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option disabled=\"disabled\" selected=\"selected\" value=\"1\">One</option>" +
                        Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        SomeGroupedSomeSelectedSelectList,
                        "<select id=\"Property1\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<optgroup label=\"Group Two\">" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<optgroup label=\"Group Two\">" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        OneGroupSomeSelectedSelectList,
                        "<select id=\"Property1\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        OneDisabledGroupAllSelectedSelectList,
                        "<select id=\"Property1\" name=\"Property1\"><optgroup disabled=\"disabled\" label=\"Disabled Group\">" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" name=\"Property1\"><optgroup disabled=\"disabled\" label=\"Disabled Group\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>"
                    },
                };
            }
        }

        // Select list -> expected HTML with null model, with model containing "2", and with model containing "1", "3".
        public static TheoryData<IEnumerable<SelectListItem>, string, string, string> ListBoxDataSet
        {
            get
            {
                return new TheoryData<IEnumerable<SelectListItem>, string, string, string>
                {
                    {
                        BasicSelectList,
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        SomeDisabledOneSelectedSelectList,
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option disabled=\"disabled\" selected=\"selected\" value=\"1\">One</option>" +
                        Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option disabled=\"disabled\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><option value=\"0\">Zero</option>" +
                        Environment.NewLine +
                        "<option disabled=\"disabled\" selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option disabled=\"disabled\" selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        SomeGroupedSomeSelectedSelectList,
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<optgroup label=\"Group Two\">" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<optgroup label=\"Group Two\">" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<optgroup label=\"Group Two\">" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        OneGroupSomeSelectedSelectList,
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\"><optgroup label=\"Group One\">" +
                        Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>"
                    },
                    {
                        OneDisabledGroupAllSelectedSelectList,
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\">" +
                        "<optgroup disabled=\"disabled\" label=\"Disabled Group\">" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\">" +
                        "<optgroup disabled=\"disabled\" label=\"Disabled Group\">" + Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option value=\"1\">One</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"2\">Two</option>" + Environment.NewLine +
                        "<option value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>",
                        "<select id=\"Property1\" multiple=\"multiple\" name=\"Property1\">" +
                        "<optgroup disabled=\"disabled\" label=\"Disabled Group\">" + Environment.NewLine +
                        "<option value=\"0\">Zero</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"1\">One</option>" + Environment.NewLine +
                        "<option value=\"2\">Two</option>" + Environment.NewLine +
                        "<option selected=\"selected\" value=\"3\">Three</option>" + Environment.NewLine +
                        "</optgroup>" + Environment.NewLine +
                        "</select>"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(DropDownListDataSet))]
        public void DropDownList_WithNullModel_GeneratesExpectedValue_DoesNotChangeSelectList(
            IEnumerable<SelectListItem> selectList,
            string expectedHtml,
            string ignoredHtml)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            var savedDisabled = selectList.Select(item => item.Disabled).ToList();
            var savedGroup = selectList.Select(item => item.Group).ToList();
            var savedSelected = selectList.Select(item => item.Selected).ToList();
            var savedText = selectList.Select(item => item.Text).ToList();
            var savedValue = selectList.Select(item => item.Value).ToList();

            // Act
            var html = helper.DropDownList("Property1", selectList, optionLabel: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedDisabled, selectList.Select(item => item.Disabled));
            Assert.Equal(savedGroup, selectList.Select(item => item.Group));
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
            Assert.Equal(savedText, selectList.Select(item => item.Text));
            Assert.Equal(savedValue, selectList.Select(item => item.Value));
        }

        [Theory]
        [MemberData(nameof(DropDownListDataSet))]
        public void DropDownList_WithModelValue_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml,
            string expectedHtml)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "2" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.DropDownList("Property1", selectList, optionLabel: null, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(DropDownListDataSet))]
        public void DropDownListFor_WithNullModel_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string expectedHtml,
            string ignoredHtml)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper();
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.DropDownListFor(
                value => value.Property1,
                selectList,
                optionLabel: null,
                htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(DropDownListDataSet))]
        public void DropDownListFor_WithModelValue_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml,
            string expectedHtml)
        {
            // Arrange
            var model = new DefaultTemplatesUtilities.ObjectTemplateModel { Property1 = "2" };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.DropDownListFor(
                value => value.Property1,
                selectList,
                optionLabel: null,
                htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBox_WithNullModel_GeneratesExpectedValue_DoesNotChangeSelectList(
            IEnumerable<SelectListItem> selectList,
            string expectedHtml,
            string ignoredHtml1,
            string ignoredHtml2)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<ModelContainingList>(model: null);
            var savedDisabled = selectList.Select(item => item.Disabled).ToList();
            var savedGroup = selectList.Select(item => item.Group).ToList();
            var savedSelected = selectList.Select(item => item.Selected).ToList();
            var savedText = selectList.Select(item => item.Text).ToList();
            var savedValue = selectList.Select(item => item.Value).ToList();

            // Act
            var html = helper.ListBox("Property1", selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedDisabled, selectList.Select(item => item.Disabled));
            Assert.Equal(savedGroup, selectList.Select(item => item.Group));
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
            Assert.Equal(savedText, selectList.Select(item => item.Text));
            Assert.Equal(savedValue, selectList.Select(item => item.Value));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBox_WithModelValue_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml1,
            string expectedHtml,
            string ignoredHtml2)
        {
            // Arrange
            var model = new ModelContainingList { Property1 = { "2" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.ListBox("Property1", selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBox_WithMultipleModelValues_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml1,
            string ignoredHtml2,
            string expectedHtml)
        {
            // Arrange
            var model = new ModelContainingList { Property1 = { "1", "3" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.ListBox("Property1", selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBoxFor_WithNullModel_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string expectedHtml,
            string ignoredHtml1,
            string ignoredHtml2)
        {
            // Arrange
            var helper = DefaultTemplatesUtilities.GetHtmlHelper<ModelContainingList>(model: null);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.ListBoxFor(value => value.Property1, selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBoxFor_WithModelValue_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml1,
            string expectedHtml,
            string ignoredHtml2)
        {
            // Arrange
            var model = new ModelContainingList { Property1 = { "2" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.ListBoxFor(value => value.Property1, selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        [Theory]
        [MemberData(nameof(ListBoxDataSet))]
        public void ListBoxFor_WithMultipleModelValues_GeneratesExpectedValue(
            IEnumerable<SelectListItem> selectList,
            string ignoredHtml1,
            string ignoredHtml2,
            string expectedHtml)
        {
            // Arrange
            var model = new ModelContainingList { Property1 = { "1", "3" } };
            var helper = DefaultTemplatesUtilities.GetHtmlHelper(model);
            var savedSelected = selectList.Select(item => item.Selected).ToList();

            // Act
            var html = helper.ListBoxFor(value => value.Property1, selectList, htmlAttributes: null);

            // Assert
            Assert.Equal(expectedHtml, html.ToString());
            Assert.Equal(savedSelected, selectList.Select(item => item.Selected));
        }

        private class ModelContainingList
        {
            public List<string> Property1 { get; } = new List<string>();
        }
    }
}
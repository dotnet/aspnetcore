// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if MOCK_SUPPORT
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Xunit;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelperTest
    {
        public static TheoryData<object, KeyValuePair<string, object>> IgnoreCaseTestData
        {
            get
            {
                return new TheoryData<object, KeyValuePair<string, object>>
                {
                    {
                        new
                        {
                            selected = true,
                            SeLeCtEd = false
                        },
                        new KeyValuePair<string, object>("selected", false)
                    },
                    {
                        new
                        {
                            SeLeCtEd = false,
                            selected = true
                        },
                        new KeyValuePair<string, object>("SeLeCtEd", true)
                    },
                    {
                        new
                        {
                            SelECTeD = false,
                            SeLECTED = true
                        },
                        new KeyValuePair<string, object>("SelECTeD", true)
                    }
                };
            }
        }

        // value, expectedString
        public static TheoryData<object, string> EncodeDynamicTestData
        {
            get
            {
                var data = new TheoryData<object, string>
                {
                    { null, string.Empty },
                    // Dynamic implementation calls the string overload when possible.
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                    { new ObjectWithToStringOverride(), "HtmlEncode[[<b>boldFromObject</b>]]" },
                };

                return data;
            }
        }

        // value, expectedString
        public static TheoryData<object, string> EncodeObjectTestData
        {
            get
            {
                var data = new TheoryData<object, string>
                {
                    { null, string.Empty },
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                    { new ObjectWithToStringOverride(), "HtmlEncode[[<b>boldFromObject</b>]]" },
                };

                return data;
            }
        }

        // value, expectedString
        public static TheoryData<string, string> EncodeStringTestData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { null, string.Empty },
                    // String overload does not encode the empty string.
                    { string.Empty, string.Empty },
                    { "<\">", "HtmlEncode[[<\">]]" },
                    { "<br />", "HtmlEncode[[<br />]]" },
                    { "<b>bold</b>", "HtmlEncode[[<b>bold</b>]]" },
                };
            }
        }

        // value, expectedString
        public static TheoryData<object, string> RawObjectTestData
        {
            get
            {
                var data = new TheoryData<object, string>
                {
                    { new ObjectWithToStringOverride(), "<b>boldFromObject</b>" },
                };

                foreach (var item in RawStringTestData)
                {
                    data.Add(item[0], (string)item[1]);
                }

                return data;
            }
        }

        // value, expectedString
        public static TheoryData<string, string> RawStringTestData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { null, null },
                    { string.Empty, string.Empty },
                    { "<\">", "<\">" },
                    { "<br />", "<br />" },
                    { "<b>bold</b>", "<b>bold</b>" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IgnoreCaseTestData))]
        public void AnonymousObjectToHtmlAttributes_IgnoresPropertyCase(
            object htmlAttributeObject,
            KeyValuePair<string, object> expectedEntry)
        {
            // Act
            var result = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributeObject);

            // Assert
            var entry = Assert.Single(result);
            Assert.Equal(expectedEntry, entry);
        }

        [Theory]
        [MemberData(nameof(EncodeDynamicTestData))]
        public void EncodeDynamic_ReturnsExpectedString(object value, string expectedString)
        {
            // Arrange
            // Important to preserve these particular variable types. Otherwise may end up testing different runtime
            // (not compiler) behaviors.
            dynamic dynamicValue = value;
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Encode(dynamicValue);

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Theory]
        [MemberData(nameof(EncodeDynamicTestData))]
        public void EncodeDynamic_ReturnsExpectedString_WithBaseHelper(object value, string expectedString)
        {
            // Arrange
            // Important to preserve these particular variable types. Otherwise may end up testing different runtime
            // (not compiler) behaviors.
            dynamic dynamicValue = value;
            IHtmlHelper helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Encode(dynamicValue);

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Theory]
        [MemberData(nameof(EncodeObjectTestData))]
        public void EncodeObject_ReturnsExpectedString(object value, string expectedString)
        {
            // Arrange
            // Important to preserve this particular variable type and the (object) type of the value parameter.
            // Otherwise may end up testing different runtime (not compiler) behaviors.
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Encode(value);

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Theory]
        [MemberData(nameof(EncodeStringTestData))]
        public void EncodeString_ReturnsExpectedString(string value, string expectedString)
        {
            // Arrange
            // Important to preserve this particular variable type and the (string) type of the value parameter.
            // Otherwise may end up testing different runtime (not compiler) behaviors.
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Encode(value);

            // Assert
            Assert.Equal(expectedString, result);
        }

        [Theory]
        [MemberData(nameof(RawObjectTestData))]
        public void RawDynamic_ReturnsExpectedString(object value, string expectedString)
        {
            // Arrange
            // Important to preserve these particular variable types. Otherwise may end up testing different runtime
            // (not compiler) behaviors.
            dynamic dynamicValue = value;
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Raw(dynamicValue);

            // Assert
            Assert.Equal(expectedString, result.ToString());
        }

        [Theory]
        [MemberData(nameof(RawObjectTestData))]
        public void RawDynamic_ReturnsExpectedString_WithBaseHelper(object value, string expectedString)
        {
            // Arrange
            // Important to preserve these particular variable types. Otherwise may end up testing different runtime
            // (not compiler) behaviors.
            dynamic dynamicValue = value;
            IHtmlHelper helper = DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Raw(dynamicValue);

            // Assert
            Assert.Equal(expectedString, result.ToString());
        }

        [Theory]
        [MemberData(nameof(RawObjectTestData))]
        public void RawObject_ReturnsExpectedString(object value, string expectedString)
        {
            // Arrange
            // Important to preserve this particular variable type and the (object) type of the value parameter.
            // Otherwise may end up testing different runtime (not compiler) behaviors.
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Raw(value);

            // Assert
            Assert.Equal(expectedString, result.ToString());
        }

        [Theory]
        [MemberData(nameof(RawStringTestData))]
        public void RawString_ReturnsExpectedString(string value, string expectedString)
        {
            // Arrange
            // Important to preserve this particular variable type and the (string) type of the value parameter.
            // Otherwise may end up testing different runtime (not compiler) behaviors.
            IHtmlHelper<DefaultTemplatesUtilities.ObjectTemplateModel> helper =
                DefaultTemplatesUtilities.GetHtmlHelper();

            // Act
            var result = helper.Raw(value);

            // Assert
            Assert.Equal(expectedString, result.ToString());
        }

        private class ObjectWithToStringOverride
        {
            public override string ToString()
            {
                return "<b>boldFromObject</b>";
            }
        }
    }
}
#endif

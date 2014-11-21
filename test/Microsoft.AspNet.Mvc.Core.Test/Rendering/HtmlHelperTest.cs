// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
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

        [Theory]
        [MemberData(nameof(IgnoreCaseTestData))]
        public void AnonymousObjectToHtmlAttributes_IgnoresPropertyCase(object htmlAttributeObject,
                                                                        KeyValuePair<string, object> expectedEntry)
        {
            // Act
            var result = HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributeObject);

            // Assert
            var entry = Assert.Single(result);
            Assert.Equal(expectedEntry, entry);
        }
    }
}

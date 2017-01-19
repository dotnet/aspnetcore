// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class TestTagHelperDescriptors
    {
        internal static IEnumerable<TagHelperDescriptor> DefaultPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: string.Empty);
        internal static IEnumerable<TagHelperDescriptor> PrefixedPAndInputTagHelperDescriptors { get; }
            = BuildPAndInputTagHelperDescriptors(prefix: "THS");

        internal static IEnumerable<TagHelperDescriptor> SimpleTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "span",
                        TypeName = "SpanTagHelper",
                        AssemblyName = "TestAssembly",
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "TestAssembly",
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "value",
                                PropertyName = "FooProp",
                                TypeName = "System.String"
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "BoundProp",
                                TypeName = "System.String"
                            }
                        }
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> CssSelectorTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");

                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "a",
                        TypeName = "TestNamespace.ATagHelper",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.FullMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "a",
                        TypeName = "TestNamespace.ATagHelperMultipleSelectors",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.PrefixMatch,
                            },
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "?hello=world",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.SuffixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "type",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "text",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.FullMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "ty",
                                NameComparison = TagHelperRequiredAttributeNameComparison.PrefixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "href",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                                Value = "~/",
                                ValueComparison = TagHelperRequiredAttributeValueComparison.PrefixMatch,
                            }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper2",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor
                            {
                                Name = "type",
                                NameComparison = TagHelperRequiredAttributeNameComparison.FullMatch,
                            }
                        },
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> EnumTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "catch-all",
                                PropertyName = "CatchAll",
                                IsEnum = true,
                                TypeName = typeof(MyEnum).FullName
                            },
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "value",
                                PropertyName = "Value",
                                IsEnum = true,
                                TypeName = typeof(MyEnum).FullName
                            },
                        }
                    },
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> SymbolBoundTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "[item]",
                                PropertyName = "ListItems",
                                TypeName = typeof(List<string>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "[(item)]",
                                PropertyName = "ArrayItems",
                                TypeName = typeof(string[]).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "(click)",
                                PropertyName = "Event1",
                                TypeName = typeof(Action).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "(^click)",
                                PropertyName = "Event2",
                                TypeName = typeof(Action).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "*something",
                                PropertyName = "StringProperty1",
                                TypeName = typeof(string).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "#local",
                                PropertyName = "StringProperty2",
                                TypeName = typeof(string).FullName
                            },
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "bound" } },
                    },
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> MinimizedTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "catchall-bound-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "catchall-unbound-required" }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "input-bound-required-string",
                                PropertyName = "BoundRequiredString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "input-bound-string",
                                PropertyName = "BoundString",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "input-bound-required-string" },
                            new TagHelperRequiredAttributeDescriptor { Name = "input-unbound-required" }
                        },
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> DynamicAttributeTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "bound",
                                PropertyName = "Bound",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            }
                        }
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> DuplicateTargetTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "checked" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "checked" } },
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> AttributeTargetingTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetProperty("Checked");
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "p",
                        TypeName = "TestNamespace.PTagHelper",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "class" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                        },
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "type" } },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "TestAssembly",
                        Attributes = new TagHelperAttributeDescriptor[]
                        {
                            new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                            new TagHelperAttributeDescriptor("checked", inputCheckedPropertyInfo)
                        },
                        RequiredAttributes = new[]
                        {
                            new TagHelperRequiredAttributeDescriptor { Name = "type" },
                            new TagHelperRequiredAttributeDescriptor { Name = "checked" }
                        },
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "*",
                        TypeName = "TestNamespace.CatchAllTagHelper",
                        AssemblyName = "TestAssembly",
                        RequiredAttributes = new[] { new TagHelperRequiredAttributeDescriptor { Name = "catchAll" } },
                    }
                };
            }
        }

        internal static IEnumerable<TagHelperDescriptor> PrefixedAttributeTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper1",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-grabber",
                                PropertyName = "IntProperty",
                                TypeName = typeof(int).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-dictionary",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(IDictionary<string, int>).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-dictionary",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = "Namespace.DictionaryWithoutParameterlessConstructor<string, string>"
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-grabber",
                                PropertyName = "StringProperty",
                                TypeName = typeof(string).FullName,
                                IsStringProperty = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName,
                                IsIndexer = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = typeof(string).FullName,
                                IsIndexer = true,
                                IsStringProperty = true
                            }
                        }
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                        TypeName = "TestNamespace.InputTagHelper2",
                        AssemblyName = "TestAssembly",
                        Attributes = new[]
                        {
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-dictionary",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-dictionary",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = "Namespace.DictionaryWithoutParameterlessConstructor<string, string>"
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "int-prefix-",
                                PropertyName = "IntDictionaryProperty",
                                TypeName = typeof(int).FullName,
                                IsIndexer = true
                            },
                            new TagHelperAttributeDescriptor
                            {
                                Name = "string-prefix-",
                                PropertyName = "StringDictionaryProperty",
                                TypeName = typeof(string).FullName,
                                IsIndexer = true,
                                IsStringProperty = true
                            }
                        }
                    }
                };
            }
        }

        private static IEnumerable<TagHelperDescriptor> BuildPAndInputTagHelperDescriptors(string prefix)
        {
            var pAgePropertyInfo = typeof(TestType).GetProperty("Age");
            var inputTypePropertyInfo = typeof(TestType).GetProperty("Type");
            var checkedPropertyInfo = typeof(TestType).GetProperty("Checked");

            return new[]
            {
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "p",
                    TypeName = "TestNamespace.PTagHelper",
                    AssemblyName = "TestAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("age", pAgePropertyInfo)
                    },
                    TagStructure = TagStructure.NormalOrSelfClosing
                },
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "input",
                    TypeName = "TestNamespace.InputTagHelper",
                    AssemblyName = "TestAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo)
                    },
                    TagStructure = TagStructure.WithoutEndTag
                },
                new TagHelperDescriptor
                {
                    Prefix = prefix,
                    TagName = "input",
                    TypeName = "TestNamespace.InputTagHelper2",
                    AssemblyName = "TestAssembly",
                    Attributes = new TagHelperAttributeDescriptor[]
                    {
                        new TagHelperAttributeDescriptor("type", inputTypePropertyInfo),
                        new TagHelperAttributeDescriptor("checked", checkedPropertyInfo)
                    },
                }
            };
        }

        private class TestType
        {
            public int Age { get; set; }

            public string Type { get; set; }

            public bool Checked { get; set; }

            public string BoundProperty { get; set; }
        }

        public enum MyEnum
        {
            MyValue,
            MySecondValue
        }
    }
}

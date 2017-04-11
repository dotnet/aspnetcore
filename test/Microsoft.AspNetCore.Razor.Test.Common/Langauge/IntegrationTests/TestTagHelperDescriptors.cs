// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class TestTagHelperDescriptors
    {
        public static IEnumerable<TagHelperDescriptor> SimpleTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "span",
                        typeName: "SpanTagHelper",
                        assemblyName: "TestAssembly"),
                    CreateTagHelperDescriptor(
                        tagName: "div",
                        typeName: "DivTagHelper",
                        assemblyName: "TestAssembly"),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("value")
                                .PropertyName("FooProp")
                                .TypeName("System.String"),
                            builder => builder
                                .Name("bound")
                                .PropertyName("BoundProp")
                                .TypeName("System.String"),
                        })
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> CssSelectorTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetRuntimeProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetRuntimeProperty("Checked");

                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "a",
                        typeName: "TestNamespace.ATagHelper",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("href")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                                    .Value("~/")
                                    .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch)),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "a",
                        typeName: "TestNamespace.ATagHelperMultipleSelectors",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("href")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                                    .Value("~/")
                                    .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch))
                                .RequireAttribute(attribute => attribute
                                    .Name("href")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                                    .Value("?hello=world")
                                    .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.SuffixMatch)),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("type")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                                    .Value("text")
                                    .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.FullMatch)),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper2",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("ty")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("href")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)
                                    .Value("~/")
                                    .ValueComparisonMode(RequiredAttributeDescriptor.ValueComparisonMode.PrefixMatch)),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper2",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute
                                    .Name("type")
                                    .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.FullMatch)),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> EnumTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("catch-all")
                                .PropertyName("CatchAll")
                                .AsEnum()
                                .TypeName(typeof(MyEnum).FullName),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("value")
                                .PropertyName("Value")
                                .AsEnum()
                                .TypeName(typeof(MyEnum).FullName),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> SymbolBoundTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("[item]")
                                .PropertyName("ListItems")
                                .TypeName(typeof(List<string>).FullName),
                            builder => builder
                                .Name("[(item)]")
                                .PropertyName("ArrayItems")
                                .TypeName(typeof(string[]).FullName),
                            builder => builder
                                .Name("(click)")
                                .PropertyName("Event1")
                                .TypeName(typeof(Action).FullName),
                            builder => builder
                                .Name("(^click)")
                                .PropertyName("Event2")
                                .TypeName(typeof(Action).FullName),
                            builder => builder
                                .Name("*something")
                                .PropertyName("StringProperty1")
                                .TypeName(typeof(string).FullName),
                            builder => builder
                                .Name("#local")
                                .PropertyName("StringProperty2")
                                .TypeName(typeof(string).FullName),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireAttribute(attribute => attribute.Name("bound")),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> MinimizedTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("catchall-bound-string")
                                .PropertyName("BoundRequiredString")
                                .TypeName(typeof(string).FullName),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireAttribute(attribute => attribute.Name("catchall-unbound-required")),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("input-bound-required-string")
                                .PropertyName("BoundRequiredString")
                                .TypeName(typeof(string).FullName),
                            builder => builder
                                .Name("input-bound-string")
                                .PropertyName("BoundString")
                                .TypeName(typeof(string).FullName),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute.Name("input-bound-required-string"))
                                .RequireAttribute(attribute => attribute.Name("input-unbound-required")),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> DynamicAttributeTagHelpers_Descriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("bound")
                                .PropertyName("Bound")
                                .TypeName(typeof(string).FullName)
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> AttributeTargetingTagHelperDescriptors
        {
            get
            {
                var inputTypePropertyInfo = typeof(TestType).GetRuntimeProperty("Type");
                var inputCheckedPropertyInfo = typeof(TestType).GetRuntimeProperty("Checked");
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "p",
                        typeName: "TestNamespace.PTagHelper",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireAttribute(attribute => attribute.Name("class")),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireAttribute(attribute => attribute.Name("type")),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper2",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", inputCheckedPropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder
                                .RequireAttribute(attribute => attribute.Name("type"))
                                .RequireAttribute(attribute => attribute.Name("checked")),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "*",
                        typeName: "TestNamespace.CatchAllTagHelper",
                        assemblyName: "TestAssembly",
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireAttribute(attribute => attribute.Name("catchAll")),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> PrefixedAttributeTagHelperDescriptors
        {
            get
            {
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper1",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("int-prefix-grabber")
                                .PropertyName("IntProperty")
                                .TypeName(typeof(int).FullName),
                            builder => builder
                                .Name("int-dictionary")
                                .PropertyName("IntDictionaryProperty")
                                .TypeName(typeof(IDictionary<string, int>).FullName)
                                .AsDictionary("int-prefix-", typeof(int).FullName),
                            builder => builder
                                .Name("string-prefix-grabber")
                                .PropertyName("StringProperty")
                                .TypeName(typeof(string).FullName),
                            builder => builder
                                .Name("string-dictionary")
                                .PropertyName("StringDictionaryProperty")
                                .TypeName("Namespace.DictionaryWithoutParameterlessConstructor<string, string>")
                                .AsDictionary("string-prefix-", typeof(string).FullName),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper2",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => builder
                                .Name("int-dictionary")
                                .PropertyName("IntDictionaryProperty")
                                .TypeName(typeof(int).FullName)
                                .AsDictionary("int-prefix-", typeof(int).FullName),
                            builder => builder
                                .Name("string-dictionary")
                                .PropertyName("StringDictionaryProperty")
                                .TypeName("Namespace.DictionaryWithoutParameterlessConstructor<string, string>")
                                .AsDictionary("string-prefix-", typeof(string).FullName),
                        }),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> TagHelpersInSectionDescriptors
        {
            get
            {
                var propertyInfo = typeof(TestType).GetRuntimeProperty("BoundProperty");
                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "MyTagHelper",
                        typeName: "TestNamespace.MyTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "BoundProperty", propertyInfo),
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "NestedTagHelper",
                        typeName: "TestNamespace.NestedTagHelper",
                        assemblyName: "TestAssembly"),
                };
            }
        }

        public static IEnumerable<TagHelperDescriptor> DefaultPAndInputTagHelperDescriptors
        {
            get
            {
                var pAgePropertyInfo = typeof(TestType).GetRuntimeProperty("Age");
                var inputTypePropertyInfo = typeof(TestType).GetRuntimeProperty("Type");
                var checkedPropertyInfo = typeof(TestType).GetRuntimeProperty("Checked");

                return new[]
                {
                    CreateTagHelperDescriptor(
                        tagName: "p",
                        typeName: "TestNamespace.PTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "age", pAgePropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireTagStructure(TagStructure.NormalOrSelfClosing)
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                        },
                        ruleBuilders: new Action<TagMatchingRuleBuilder>[]
                        {
                            builder => builder.RequireTagStructure(TagStructure.WithoutEndTag)
                        }),
                    CreateTagHelperDescriptor(
                        tagName: "input",
                        typeName: "TestNamespace.InputTagHelper2",
                        assemblyName: "TestAssembly",
                        attributes: new Action<ITagHelperBoundAttributeDescriptorBuilder>[]
                        {
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "type", inputTypePropertyInfo),
                            builder => BuildBoundAttributeDescriptorFromPropertyInfo(builder, "checked", checkedPropertyInfo),
                        }),
                };
            }
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<ITagHelperBoundAttributeDescriptorBuilder>> attributes = null,
            IEnumerable<Action<TagMatchingRuleBuilder>> ruleBuilders = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            if (ruleBuilders != null)
            {
                foreach (var ruleBuilder in ruleBuilders)
                {
                    builder.TagMatchingRule(innerRuleBuilder => {
                        innerRuleBuilder.RequireTagName(tagName);
                        ruleBuilder(innerRuleBuilder);
                    });
                }
            }
            else
            {
                builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));
            }

            var descriptor = builder.Build();

            return descriptor;
        }

        private static void BuildBoundAttributeDescriptorFromPropertyInfo(
            ITagHelperBoundAttributeDescriptorBuilder builder,
            string name,
            PropertyInfo propertyInfo)
        {
            builder
                .Name(name)
                .PropertyName(propertyInfo.Name)
                .TypeName(propertyInfo.PropertyType.FullName);

            if (propertyInfo.PropertyType.GetTypeInfo().IsEnum)
            {
                builder.AsEnum();
            }
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

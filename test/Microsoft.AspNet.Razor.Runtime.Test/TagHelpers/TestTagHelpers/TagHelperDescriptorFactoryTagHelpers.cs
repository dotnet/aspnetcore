// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{

    [RestrictChildren("p")]
    public class RestrictChildrenTagHelper
    {
    }

    [RestrictChildren("p", "strong")]
    public class DoubleRestrictChildrenTagHelper
    {
    }

    [TargetElement("p")]
    [TargetElement("div")]
    [RestrictChildren("p", "strong")]
    public class MultiTargetRestrictChildrenTagHelper
    {
    }

    [TargetElement("input", TagStructure = TagStructure.WithoutEndTag)]
    public class TagStructureTagHelper : TagHelper
    {
    }

    [TargetElement("p", TagStructure = TagStructure.NormalOrSelfClosing)]
    [TargetElement("input", TagStructure = TagStructure.WithoutEndTag)]
    public class MultiSpecifiedTagStructureTagHelper : TagHelper
    {
    }

    [TargetElement("p")]
    [TargetElement("input", TagStructure = TagStructure.WithoutEndTag)]
    public class MultiWithUnspecifiedTagStructureTagHelper : TagHelper
    {
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public class DefaultEditorBrowsableTagHelper : TagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Always)]
        public int Property { get; set; }
    }

    public class HiddenPropertyEditorBrowsableTagHelper : TagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int Property { get; set; }
    }

    public class MultiPropertyEditorBrowsableTagHelper : TagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int Property { get; set; }

        public virtual int Property2 { get; set; }
    }

    public class OverriddenPropertyEditorBrowsableTagHelper : MultiPropertyEditorBrowsableTagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int Property2 { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public class EditorBrowsableTagHelper : TagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual int Property { get; set; }
    }

    public class InheritedEditorBrowsableTagHelper : EditorBrowsableTagHelper
    {
        public override int Property { get; set; }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public class OverriddenEditorBrowsableTagHelper : EditorBrowsableTagHelper
    {
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public override int Property { get; set; }
    }

    [TargetElement("p")]
    [TargetElement("div")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MultiEditorBrowsableTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "class*")]
    public class AttributeWildcardTargetingTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "class*,style*")]
    public class MultiAttributeWildcardTargetingTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "class")]
    public class AttributeTargetingTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "class,style")]
    public class MultiAttributeTargetingTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "custom")]
    [TargetElement(Attributes = "class,style")]
    public class MultiAttributeAttributeTargetingTagHelper : TagHelper
    {
    }

    [TargetElement(Attributes = "style")]
    public class InheritedAttributeTargetingTagHelper : AttributeTargetingTagHelper
    {
    }

    [TargetElement("input", Attributes = "class")]
    public class RequiredAttributeTagHelper : TagHelper
    {
    }

    [TargetElement("div", Attributes = "class")]
    public class InheritedRequiredAttributeTagHelper : RequiredAttributeTagHelper
    {
    }

    [TargetElement("div", Attributes = "class")]
    [TargetElement("input", Attributes = "class")]
    public class MultiAttributeRequiredAttributeTagHelper : TagHelper
    {
    }

    [TargetElement("input", Attributes = "style")]
    [TargetElement("input", Attributes = "class")]
    public class MultiAttributeSameTagRequiredAttributeTagHelper : TagHelper
    {
    }

    [TargetElement("input", Attributes = "class,style")]
    public class MultiRequiredAttributeTagHelper : TagHelper
    {
    }

    [TargetElement("div", Attributes = "style")]
    public class InheritedMultiRequiredAttributeTagHelper : MultiRequiredAttributeTagHelper
    {
    }

    [TargetElement("div", Attributes = "class,style")]
    [TargetElement("input", Attributes = "class,style")]
    public class MultiTagMultiRequiredAttributeTagHelper : TagHelper
    {
    }

    [TargetElement("p")]
    [TargetElement("div")]
    public class MultiTagTagHelper
    {
        public string ValidAttribute { get; set; }
    }

    public class InheritedMultiTagTagHelper : MultiTagTagHelper
    {
    }

    [TargetElement("p")]
    [TargetElement("p")]
    [TargetElement("div")]
    [TargetElement("div")]
    public class DuplicateTagNameTagHelper
    {
    }

    [TargetElement("data-condition")]
    public class OverrideNameTagHelper
    {
    }

    public class InheritedSingleAttributeTagHelper : SingleAttributeTagHelper
    {
    }

    public class DuplicateAttributeNameTagHelper
    {
        public string MyNameIsLegion { get; set; }

        [HtmlAttributeName("my-name-is-legion")]
        public string Fred { get; set; }
    }

    public class NotBoundAttributeTagHelper
    {
        public object BoundProperty { get; set; }

        [HtmlAttributeNotBound]
        public string NotBoundProperty { get; set; }

        [HtmlAttributeName("unused")]
        [HtmlAttributeNotBound]
        public string NamedNotBoundProperty { get; set; }
    }

    public class OverriddenAttributeTagHelper
    {
        [HtmlAttributeName("SomethingElse")]
        public virtual string ValidAttribute1 { get; set; }

        [HtmlAttributeName("Something-Else")]
        public string ValidAttribute2 { get; set; }
    }

    public class InheritedOverriddenAttributeTagHelper : OverriddenAttributeTagHelper
    {
        public override string ValidAttribute1 { get; set; }
    }

    public class InheritedNotOverriddenAttributeTagHelper : OverriddenAttributeTagHelper
    {
    }

    public class ALLCAPSTAGHELPER : TagHelper
    {
        public int ALLCAPSATTRIBUTE { get; set; }
    }

    public class CAPSOnOUTSIDETagHelper : TagHelper
    {
        public int CAPSOnOUTSIDEATTRIBUTE { get; set; }
    }

    public class capsONInsideTagHelper : TagHelper
    {
        public int capsONInsideattribute { get; set; }
    }

    public class One1Two2Three3TagHelper : TagHelper
    {
        public int One1Two2Three3Attribute { get; set; }
    }

    public class ONE1TWO2THREE3TagHelper : TagHelper
    {
        public int ONE1TWO2THREE3Attribute { get; set; }
    }

    public class First_Second_ThirdHiTagHelper : TagHelper
    {
        public int First_Second_ThirdAttribute { get; set; }
    }

    public class UNSuffixedCLASS : TagHelper
    {
        public int UNSuffixedATTRIBUTE { get; set; }
    }

    public class InvalidBoundAttribute : TagHelper
    {
        public string DataSomething { get; set; }
    }

    public class InvalidBoundAttributeWithValid : SingleAttributeTagHelper
    {
        public string DataSomething { get; set; }
    }

    public class OverriddenInvalidBoundAttributeWithValid : TagHelper
    {
        [HtmlAttributeName("valid-something")]
        public string DataSomething { get; set; }
    }

    public class OverriddenValidBoundAttributeWithInvalid : TagHelper
    {
        [HtmlAttributeName("data-something")]
        public string ValidSomething { get; set; }
    }

    public class OverriddenValidBoundAttributeWithInvalidUpperCase : TagHelper
    {
        [HtmlAttributeName("DATA-SOMETHING")]
        public string ValidSomething { get; set; }
    }

    public class DefaultValidHtmlAttributePrefix : TagHelper
    {
        public IDictionary<string, string> DictionaryProperty { get; set; }
    }

    public class SingleValidHtmlAttributePrefix : TagHelper
    {
        [HtmlAttributeName("valid-name")]
        public IDictionary<string, string> DictionaryProperty { get; set; }
    }

    public class MultipleValidHtmlAttributePrefix : TagHelper
    {
        [HtmlAttributeName("valid-name1", DictionaryAttributePrefix = "valid-prefix1-")]
        public Dictionary<string, object> DictionaryProperty { get; set; }

        [HtmlAttributeName("valid-name2", DictionaryAttributePrefix = "valid-prefix2-")]
        public DictionarySubclass DictionarySubclassProperty { get; set; }

        [HtmlAttributeName("valid-name3", DictionaryAttributePrefix = "valid-prefix3-")]
        public DictionaryWithoutParameterlessConstructor DictionaryWithoutParameterlessConstructorProperty { get; set; }

        [HtmlAttributeName("valid-name4", DictionaryAttributePrefix = "valid-prefix4-")]
        public GenericDictionarySubclass<object> GenericDictionarySubclassProperty { get; set; }

        [HtmlAttributeName("valid-name5", DictionaryAttributePrefix = "valid-prefix5-")]
        public SortedDictionary<string, int> SortedDictionaryProperty { get; set; }

        [HtmlAttributeName("valid-name6")]
        public string StringProperty { get; set; }

        public IDictionary<string, int> GetOnlyDictionaryProperty { get; }

        [HtmlAttributeName(DictionaryAttributePrefix = "valid-prefix6")]
        public IDictionary<string, string> GetOnlyDictionaryPropertyWithAttributePrefix { get; }
    }

    public class SingleInvalidHtmlAttributePrefix : TagHelper
    {
        [HtmlAttributeName("valid-name", DictionaryAttributePrefix = "valid-prefix")]
        public string StringProperty { get; set; }
    }

    public class MultipleInvalidHtmlAttributePrefix : TagHelper
    {
        [HtmlAttributeName("valid-name1")]
        public long LongProperty { get; set; }

        [HtmlAttributeName("valid-name2", DictionaryAttributePrefix = "valid-prefix2-")]
        public Dictionary<int, string> DictionaryOfIntProperty { get; set; }

        [HtmlAttributeName("valid-name3", DictionaryAttributePrefix = "valid-prefix3-")]
        public IReadOnlyDictionary<string, object> ReadOnlyDictionaryProperty { get; set; }

        [HtmlAttributeName("valid-name4", DictionaryAttributePrefix = "valid-prefix4-")]
        public int IntProperty { get; set; }

        [HtmlAttributeName("valid-name5", DictionaryAttributePrefix = "valid-prefix5-")]
        public DictionaryOfIntSubclass DictionaryOfIntSubclassProperty { get; set; }

        [HtmlAttributeName(DictionaryAttributePrefix = "valid-prefix6")]
        public IDictionary<int, string> GetOnlyDictionaryAttributePrefix { get; }

        [HtmlAttributeName("invalid-name7")]
        public IDictionary<string, object> GetOnlyDictionaryPropertyWithAttributeName { get; }
    }

    public class DictionarySubclass : Dictionary<string, string>
    {
    }

    public class DictionaryWithoutParameterlessConstructor : Dictionary<string, string>
    {
        public DictionaryWithoutParameterlessConstructor(int count)
            : base()
        {
        }
    }

    public class DictionaryOfIntSubclass : Dictionary<int, string>
    {
    }

    public class GenericDictionarySubclass<TValue> : Dictionary<string, TValue>
    {
    }

    [OutputElementHint("strong")]
    public class OutputElementHintTagHelper : TagHelper
    {
    }

    [TargetElement("a")]
    [TargetElement("p")]
    [OutputElementHint("div")]
    public class MulitpleDescriptorTagHelperWithOutputElementHint : TagHelper
    {
    }

    public class TypeDerivingFromITagHelper : ITagHelper
    {
        public int Order { get; } = 0;

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            throw new NotImplementedException();
        }
    }

    public class BaseAttribute : Attribute
    {
        public string BaseProperty { get; set; }
    }

    public class DerivedAttribute : BaseAttribute
    {
        public string DerivedProperty { get; set; }
    }

    public class BaseTagHelper : TagHelper
    {
        [Derived(DerivedProperty = "DerivedPropertyValue")]
        public string BaseProperty { get; set; }

        [HtmlAttributeNotBound]
        public virtual string VirtualProperty { get; set; }

        public int NewProperty { get; set; }

        public virtual new string Order { get; set; }
    }

    public class DerivedTagHelper : BaseTagHelper
    {
        public override string VirtualProperty { get; set; }

        [Base(BaseProperty = "BaseValue")]
        public string DerivedProperty { get; set; }

        [HtmlAttributeName("new-property")]
        public new Type NewProperty { get; set; }

        public override string Order { get; set; }
    }
}

public class TagHelperInGlobalNamespace : TagHelper
{
}

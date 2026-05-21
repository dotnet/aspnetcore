// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;

namespace Microsoft.AspNetCore.Components;

public class FormMappingContextTest
{
    [Fact]
    public void CanCreate_MappingContext_WithDefaultName()
    {
        var context = new FormMappingContext("");
        Assert.Equal("", context.MappingScopeName);
    }

    [Fact]
    public void CanCreate_MappingContext_WithName()
    {
        var context = new FormMappingContext("name");
        Assert.Equal("name", context.MappingScopeName);
    }

    [Fact]
    public void GetFormPostValue_ErrorHandler_UsesCorrectKeyForChildObjectErrors()
    {
        var mappingContext = new FormMappingContext("");
        var formName = "myForm";
        var attribute = new SupplyParameterFromFormAttribute { FormName = formName };
        var parameterInfo = new CascadingParameterInfo(attribute, "Model", typeof(TestModelWithChild));

        var parentKey = "Model.SubModels[0]";
        var childErrorKey = "Model.SubModels[0].SubModelNumber";

        var mapper = new ErrorTriggeringFormValueMapper(childErrorKey, parentKey);

        var result = SupplyParameterFromFormValueProvider.GetFormPostValue(mapper, mappingContext, parameterInfo);

        var error = mappingContext.GetErrors(formName, childErrorKey);
        Assert.NotNull(error);
        Assert.Equal("container", error.Container);
    }

    private class ErrorTriggeringFormValueMapper : IFormValueMapper
    {
        private readonly string _errorKey;
        private readonly string _parentKey;

        public ErrorTriggeringFormValueMapper(string errorKey, string parentKey)
        {
            _errorKey = errorKey;
            _parentKey = parentKey;
        }

        public bool CanMap(Type valueType, string mappingScopeName, string formName) => true;

        public void Map(FormValueMappingContext context)
        {
            context.OnError!(_errorKey, $"The value '' is not valid for '{_errorKey}'.", "");
            context.MapErrorToContainer!(_parentKey, "container");
        }
    }

    private class TestModelWithChild
    {
        public string Name { get; set; }
        public TestSubModel[] SubModels { get; set; }
    }

    private class TestSubModel
    {
        public int SubModelNumber { get; set; }
    }
}

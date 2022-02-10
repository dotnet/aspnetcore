// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

// For E2E testing, this is a rough example of a field CSS class provider that looks for
// a custom attribute defining CSS class names. It isn't very efficient (it does reflection
// and allocates on every invocation) but is sufficient for testing purposes.
public class CustomFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var cssClassName = base.GetFieldCssClass(editContext, fieldIdentifier);

        // If we can find a [CustomValidationClassName], use it
        var propertyInfo = fieldIdentifier.Model.GetType().GetProperty(fieldIdentifier.FieldName);
        if (propertyInfo != null)
        {
            var customValidationClassName = (CustomValidationClassNameAttribute)propertyInfo
                .GetCustomAttributes(typeof(CustomValidationClassNameAttribute), true)
                .FirstOrDefault();
            if (customValidationClassName != null)
            {
                cssClassName = string.Join(' ', cssClassName.Split(' ').Select(token => token switch
                {
                    "valid" => customValidationClassName.Valid ?? token,
                    "invalid" => customValidationClassName.Invalid ?? token,
                    _ => token,
                }));
            }
        }

        return cssClassName;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class CustomValidationClassNameAttribute : Attribute
{
    public string Valid { get; set; }
    public string Invalid { get; set; }
}

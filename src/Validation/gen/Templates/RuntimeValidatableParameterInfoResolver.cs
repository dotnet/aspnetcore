file sealed class RuntimeValidatableParameterInfoResolver : global::Microsoft.Extensions.Validation.IValidatableInfoResolver
{
    // TODO: the implementation currently relies on static discovery of types.
    public bool TryGetValidatableTypeInfo(global::System.Type type, out global::Microsoft.Extensions.Validation.IValidatableTypeInfo? validatableTypeInfo)
    {
        validatableTypeInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(global::System.Reflection.ParameterInfo parameterInfo, out global::Microsoft.Extensions.Validation.IValidatableParameterInfo? validatableParameterInfo)
    {
        if (parameterInfo.Name == null)
        {
            throw new global::System.InvalidOperationException($"Encountered a parameter of type '{parameterInfo.ParameterType}' without a name. Parameters must have a name.");
        }

        // Skip method parameter if it or its type are annotated with SkipValidationAttribute.
        if (global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::Microsoft.Extensions.Validation.SkipValidationAttribute>(parameterInfo) != null ||
            global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::Microsoft.Extensions.Validation.SkipValidationAttribute>(parameterInfo.ParameterType) != null)
        {
            validatableParameterInfo = null;
            return false;
        }

        var validationAttributes = global::System.Linq.Enumerable.ToArray(global::System.Reflection.CustomAttributeExtensions.GetCustomAttributes<global::System.ComponentModel.DataAnnotations.ValidationAttribute>(parameterInfo));

        // If there are no validation attributes and this type is not a complex type
        // we don't need to validate it. Complex types without attributes are still
        // validatable because we want to run the validations on the properties.
        if (validationAttributes.Length == 0 && !IsComplexType(parameterInfo.ParameterType))
        {
            validatableParameterInfo = null;
            return false;
        }

        var displayNameInfo = ResolveDisplayInfo(parameterInfo);

        validatableParameterInfo = new RuntimeValidatableParameterInfo(
            parameterType: parameterInfo.ParameterType,
            name: parameterInfo.Name,
            displayNameInfo: displayNameInfo,
            validationAttributes: validationAttributes
        );
        return true;
    }

    private static DisplayNameInfo? ResolveDisplayInfo(global::System.Reflection.ParameterInfo parameterInfo)
    {
        var displayAttribute = global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::System.ComponentModel.DataAnnotations.DisplayAttribute>(parameterInfo);
        if (displayAttribute is { ResourceType: not null, Name: not null })
        {
            // Resource-based display name from [Display(ResourceType = ..., Name = ...)] is the
            // canonical localized source; the IValidationLocalizer is intentionally bypassed.
            // The DisplayAttribute instance is retained for the lifetime of the resolver, mirroring
            // the source-generator's static accessor design.
            return new ParameterReflectionDisplayName(displayAttribute);
        }

        if (displayAttribute?.Name is not null)
        {
            // Literal name from [Display(Name = "...")].
            return new LiteralDisplayName(displayAttribute.Name);
        }

        var displayNameAttribute = global::System.Reflection.CustomAttributeExtensions.GetCustomAttribute<global::System.ComponentModel.DisplayNameAttribute>(parameterInfo);
        if (displayNameAttribute is not null)
        {
            // Literal name from [DisplayName("...")].
            return new LiteralDisplayName(displayNameAttribute.DisplayName);
        }

        return null;
    }

    internal sealed class RuntimeValidatableParameterInfo(
        global::System.Type parameterType,
        string name,
        DisplayNameInfo? displayNameInfo,
        global::System.ComponentModel.DataAnnotations.ValidationAttribute[] validationAttributes) :
            ValidatableParameterInfo(parameterType, name, displayNameInfo)
    {
        protected override global::System.ComponentModel.DataAnnotations.ValidationAttribute[] GetValidationAttributes() => _validationAttributes;

        private readonly global::System.ComponentModel.DataAnnotations.ValidationAttribute[] _validationAttributes = validationAttributes;
    }

    private sealed class LiteralDisplayName(string literal) : DisplayNameInfo
    {
        public override string? GetDisplayName(global::Microsoft.Extensions.Validation.ValidateContext context, string memberName, global::System.Type? type)
        {
            var localizer = context.ValidationOptions.Localizer;
            if (localizer is null)
            {
                return literal;
            }

            // The literal acts as both the lookup key for the localizer AND the fallback display
            // name when the localizer can't translate.
            return localizer.ResolveDisplayName(new global::Microsoft.Extensions.Validation.DisplayNameLocalizationContext
            {
                Type = type,
                DisplayName = literal,
                MemberName = memberName,
            }) ?? literal;
        }
    }

    private sealed class ParameterReflectionDisplayName(global::System.ComponentModel.DataAnnotations.DisplayAttribute attribute) : DisplayNameInfo
    {
        public override string? GetDisplayName(global::Microsoft.Extensions.Validation.ValidateContext context, string memberName, global::System.Type? type)
            => attribute.GetName();
    }

    private static bool IsComplexType(global::System.Type type)
    {
        // Skip primitives, enums, common built-in types, and types that are specially
        // handled by RDF/RDG that don't need validation if they don't have attributes
        if (type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(global::System.DateTime) ||
            type == typeof(global::System.DateTimeOffset) ||
            type == typeof(global::System.TimeOnly) ||
            type == typeof(global::System.DateOnly) ||
            type == typeof(global::System.TimeSpan) ||
            type == typeof(global::System.Guid) ||
            type == typeof(global::System.Security.Claims.ClaimsPrincipal) ||
            type == typeof(global::System.Threading.CancellationToken) ||
            type == typeof(global::System.IO.Stream) ||
            type == typeof(global::System.IO.Pipelines.PipeReader))
        {
            return false;
        }

        // Check if the underlying type in a nullable is valid
        if (global::System.Nullable.GetUnderlyingType(type) is { } nullableType)
        {
            return IsComplexType(nullableType);
        }

        // Complex types include both reference types (classes) and value types (structs, record structs)
        // that aren't in the exclusion list above
        return type.IsClass || type.IsValueType;
    }
}

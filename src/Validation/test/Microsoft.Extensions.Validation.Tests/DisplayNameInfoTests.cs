// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Validation.Tests;

/// <summary>
/// Direct contract tests for <see cref="DisplayNameInfo"/> integration with the validation
/// pipeline. Verifies that custom <see cref="DisplayNameInfo"/> subclasses are invoked with
/// the right arguments, and that null returns fall back to the CLR member name.
/// </summary>
public class DisplayNameInfoTests
{
    [Fact]
    public async Task Property_DisplayNameInfo_InvokedWithMemberNameAndDeclaringType()
    {
        var captured = new List<(string MemberName, Type? DeclaringType)>();
        var displayNameInfo = new CapturingDisplayNameInfo(captured, returnValue: "Localized");

        var model = new Person { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(Person),
        [
            new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo, [new RequiredAttribute()])
        ]);
        var context = CreateContext(model);

        await typeInfo.ValidateAsync(model, context, default);

        var call = Assert.Single(captured);
        Assert.Equal("Name", call.MemberName);
        Assert.Equal(typeof(Person), call.DeclaringType);
    }

    [Fact]
    public async Task Parameter_DisplayNameInfo_InvokedWithNullDeclaringType()
    {
        var captured = new List<(string MemberName, Type? DeclaringType)>();
        var displayNameInfo = new CapturingDisplayNameInfo(captured, returnValue: "Localized");

        var paramInfo = new CapturingParameterInfo(typeof(string), "myParam", displayNameInfo, [new RequiredAttribute()]);
        var context = CreateContext(model: new object());

        await paramInfo.ValidateAsync(null, context, default);

        var call = Assert.Single(captured);
        Assert.Equal("myParam", call.MemberName);
        Assert.Null(call.DeclaringType);
    }

    [Fact]
    public async Task TypeLevelAttribute_DisplayNameInfo_InvokedWithTypeNameAndType()
    {
        var captured = new List<(string MemberName, Type? DeclaringType)>();
        var displayNameInfo = new CapturingDisplayNameInfo(captured, returnValue: "Localized Range");

        var model = new RangeModel { Start = 10, End = 5 };
        var typeInfo = new CapturingTypeInfo(
            typeof(RangeModel),
            [],
            displayNameInfo,
            [new StartLessThanEndAttribute { ErrorMessage = "Start must be less than End." }]);
        var context = CreateContext(model);

        await typeInfo.ValidateAsync(model, context, default);

        var call = Assert.Single(captured);
        Assert.Equal(nameof(RangeModel), call.MemberName);
        Assert.Equal(typeof(RangeModel), call.DeclaringType);
    }

    [Fact]
    public async Task Property_DisplayNameInfo_ReturnsNull_FallsBackToMemberNameInErrorMessage()
    {
        var displayNameInfo = new ConstantDisplayNameInfo(null);
        var model = new Person { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(Person),
        [
            new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo, [new RequiredAttribute()])
        ]);
        var context = CreateContext(model);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].Single());
    }

    [Fact]
    public async Task Property_NoDisplayNameInfo_UsesMemberNameDirectly()
    {
        var model = new Person { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(Person),
        [
            new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo: null, [new RequiredAttribute()])
        ]);
        var context = CreateContext(model);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Name field is required.", context.ValidationErrors["Name"].Single());
    }

    [Fact]
    public async Task Property_DisplayNameInfo_ReturnsValue_UsedInErrorMessage()
    {
        var displayNameInfo = new ConstantDisplayNameInfo("Custom Resolved Name");
        var model = new Person { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(Person),
        [
            new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo, [new RequiredAttribute()])
        ]);
        var context = CreateContext(model);

        await typeInfo.ValidateAsync(model, context, default);

        Assert.NotNull(context.ValidationErrors);
        Assert.Equal("The Custom Resolved Name field is required.", context.ValidationErrors["Name"].Single());
    }

    [Fact]
    public async Task Property_DisplayNameInfo_Throws_PropagatesException()
    {
        var thrown = new InvalidOperationException("Custom strategy failure");
        var displayNameInfo = new ThrowingDisplayNameInfo(thrown);
        var model = new Person { Name = null };
        var typeInfo = new TestValidatableTypeInfo(typeof(Person),
        [
            new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo, [new RequiredAttribute()])
        ]);
        var context = CreateContext(model);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => typeInfo.ValidateAsync(model, context, default));
        Assert.Same(thrown, ex);
    }

    [Fact]
    public void Constructor_StoresDisplayNameInfoOnPropertyInfo()
    {
        var info = new ConstantDisplayNameInfo("X");
        var sut = new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", info, []);

        Assert.Same(info, sut.DisplayNameInfo);
    }

    [Fact]
    public void Constructor_StoresDisplayNameInfoOnParameterInfo()
    {
        var info = new ConstantDisplayNameInfo("X");
        var sut = new CapturingParameterInfo(typeof(string), "p", info, []);

        Assert.Same(info, sut.DisplayNameInfo);
    }

    [Fact]
    public void Constructor_StoresDisplayNameInfoOnTypeInfo()
    {
        var info = new ConstantDisplayNameInfo("X");
        var sut = new CapturingTypeInfo(typeof(Person), [], info, []);

        Assert.Same(info, sut.DisplayNameInfo);
    }

    [Fact]
    public void Constructor_DefaultDisplayNameInfoIsNullOnPropertyInfo()
    {
        var sut = new CapturingPropertyInfo(typeof(Person), typeof(string), "Name", displayNameInfo: null, []);

        Assert.Null(sut.DisplayNameInfo);
    }

    private static ValidateContext CreateContext(object model)
    {
        return new ValidateContext
        {
            ValidationOptions = new ValidationOptions(),
            ValidationContext = new ValidationContext(model),
        };
    }

    private sealed class CapturingPropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _attributes;

        public CapturingPropertyInfo(
            Type declaringType,
            Type propertyType,
            string name,
            DisplayNameInfo? displayNameInfo,
            ValidationAttribute[] attributes)
            : base(declaringType, propertyType, name, displayNameInfo)
        {
            _attributes = attributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    private sealed class CapturingParameterInfo : ValidatableParameterInfo
    {
        private readonly ValidationAttribute[] _attributes;

        public CapturingParameterInfo(
            Type parameterType,
            string name,
            DisplayNameInfo? displayNameInfo,
            ValidationAttribute[] attributes)
            : base(parameterType, name, displayNameInfo)
        {
            _attributes = attributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    private sealed class CapturingTypeInfo : ValidatableTypeInfo
    {
        private readonly ValidationAttribute[] _attributes;

        public CapturingTypeInfo(
            Type type,
            ValidatablePropertyInfo[] members,
            DisplayNameInfo? displayNameInfo,
            ValidationAttribute[] attributes)
            : base(type, members, displayNameInfo)
        {
            _attributes = attributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    private sealed class CapturingDisplayNameInfo(List<(string MemberName, Type? DeclaringType)> log, string? returnValue) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
        {
            log.Add((memberName, declaringType));
            return returnValue;
        }
    }

    private sealed class ConstantDisplayNameInfo(string? value) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
            => value;
    }

    private sealed class ThrowingDisplayNameInfo(Exception exception) : DisplayNameInfo
    {
        public override string? GetDisplayName(ValidateContext context, string memberName, Type? declaringType)
            => throw exception;
    }

    private sealed class Person
    {
        public string? Name { get; set; }
    }

    private sealed class RangeModel
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class StartLessThanEndAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is RangeModel model && model.Start >= model.End)
            {
                return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Validation.Tests;

public class AsyncValidationTests
{
    [Fact]
    public async Task AsyncValidationAttribute_ValidatesAsynchronously_FailsWhenInvalid()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncUser>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncUser { Email = "duplicate@example.com" }, context, default);

        Assert.Equal("Email already exists", Assert.Single(context.ValidationErrors!["Email"]).ErrorMessage);
    }

    [Fact]
    public async Task AsyncValidationAttribute_PassesWhenValid()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncUser>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncUser { Email = "unique@example.com" }, context, default);

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Fact]
    public async Task MixedSyncAndAsyncAttributes_AllValidate()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncProduct>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncProduct { Name = null, SKU = "DUPLICATE" }, context, default);

        Assert.Equal("The Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("SKU already exists", context.ValidationErrors!["SKU"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_RespectsValidationOrder()
    {
        TrackingAsyncAttribute.CallCount = 0;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<RequiredBeforeAsyncModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new RequiredBeforeAsyncModel { Value = null }, context, default);

        Assert.Equal(0, TrackingAsyncAttribute.CallCount);
        Assert.Equal("The Value field is required.", context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task IAsyncValidatableObject_ValidatesAsynchronously()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncValidatableAccount>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncValidatableAccount { Username = "taken", Email = "duplicate@example.com" }, context, default);

        Assert.Equal("Username is already taken", context.ValidationErrors!["Username"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("Email is already registered", context.ValidationErrors!["Email"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task IAsyncValidatableObject_WithMultipleMemberNames()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncRegistrationForm>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncRegistrationForm { Password = "a", ConfirmPassword = "b" }, context, default);

        Assert.Equal("Passwords do not match", context.ValidationErrors!["Password"].Select(e => e.ErrorMessage).Single());
        Assert.Equal("Passwords do not match", context.ValidationErrors!["ConfirmPassword"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_OnNestedObjects()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncOrder>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncOrder { Customer = new AsyncUser { Email = "duplicate@example.com" } }, context, default);

        Assert.Equal("Email already exists", context.ValidationErrors!["Customer.Email"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_WithCancellation()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<CancellableAsyncModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => typeInfo.ValidateAsync(new CancellableAsyncModel { Value = "x" }, context, cts.Token));
    }

    [Fact]
    public async Task AsyncValidation_TypeLevelAttribute()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncTypeLevelModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncTypeLevelModel { Value = "bad" }, context, default);

        Assert.Equal("Type-level async error", Assert.Single(context.ValidationErrors!).Value.Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_InCollection()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncUserCollection>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncUserCollection { Users = [new() { Email = "unique@example.com" }, new() { Email = "duplicate@example.com" }] }, context, default);

        Assert.Equal("Email already exists", context.ValidationErrors!["Users[1].Email"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_HandlesExceptions()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ThrowingAsyncModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => typeInfo.ValidateAsync(new ThrowingAsyncModel { Value = "x" }, context, default));

        Assert.Equal("Async validation exception", ex.Message);
    }

    [Fact]
    public async Task AsyncValidation_CombinedWithIAsyncValidatableObject()
    {
        AsyncValidatableWithPropertyError.AsyncCalls = 0;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncValidatableWithPropertyError>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncValidatableWithPropertyError { Email = "duplicate@example.com" }, context, default);

        Assert.Equal(0, AsyncValidatableWithPropertyError.AsyncCalls);
        Assert.Equal("Email already exists", context.ValidationErrors!["Email"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task IAsyncValidatableObject_RunsWhenPropertyValidationPasses()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncValidatableWithPropertyError>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncValidatableWithPropertyError { Email = "unique@example.com", Bio = "short" }, context, default);

        Assert.Equal("Bio is too short", context.ValidationErrors!["Bio"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_OnNestedIAsyncValidatableObjectProperty()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<NestedAsyncProfileContainer>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new NestedAsyncProfileContainer { Profile = new AsyncProfile { Bio = "short" } }, context, default);

        Assert.Equal("Bio is too short", context.ValidationErrors!["Profile.Bio"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_OnNestedIAsyncValidatableObjectProperty_UsesIsolatedContext()
    {
        GatedAsyncProfile.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<TwoNestedAsyncProfiles>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new TwoNestedAsyncProfiles { First = new() { Bio = "short" }, Second = new() { Bio = "short" } }, context, default);
        await GatedAsyncProfile.WaitForStartsAsync(2);
        GatedAsyncProfile.Gate.SetResult();
        await task;

        Assert.Contains("First.Bio", context.ValidationErrors!.Keys);
        Assert.Contains("Second.Bio", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task SiblingMember_WithSyncError_HasCorrectPath_WhenPrecedingMemberSuspendsAsync()
    {
        GatedSuccessAttribute.Reset(expectedStarts: 2);
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GatedThenRequiredModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new GatedThenRequiredModel { First = "ok" }, context, default);
        await GatedSuccessAttribute.WaitForStartsAsync(1);
        GatedSuccessAttribute.Gate.SetResult();
        await task;

        Assert.Contains("Second", context.ValidationErrors!.Keys);
        Assert.DoesNotContain("First.Second", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task SiblingMember_WithSyncError_HasCorrectPath_WhenPrecedingComplexMemberSuspendsAsync()
    {
        GatedSuccessAttribute.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<GatedComplexThenRequiredModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new GatedComplexThenRequiredModel { First = new() { Value = "ok" } }, context, default);
        await GatedSuccessAttribute.WaitForStartsAsync(1);
        GatedSuccessAttribute.Gate.SetResult();
        await task;

        Assert.Contains("Second", context.ValidationErrors!.Keys);
        Assert.DoesNotContain("First.Second", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task MultipleAsyncValidation_WithCancellation()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<MultipleCancellableAsyncModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(() => typeInfo.ValidateAsync(new MultipleCancellableAsyncModel { Value = "x" }, context, cts.Token));
    }

    [Fact]
    public async Task AsyncValidation_WithDelayedValidation()
    {
        DelayedFailAttribute.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DelayedValidationModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new DelayedValidationModel { Value = "x" }, context, default);
        await DelayedFailAttribute.WaitForStartsAsync(1);
        Assert.False(task.IsCompleted);
        DelayedFailAttribute.Gate.SetResult();
        await task;

        Assert.Equal("Delayed validation failed", context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_MultipleAttributesOnSameProperty_ShortCircuitsAfterFirstError()
    {
        NeverCompletesUnlessCanceledAttribute.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ShortCircuitSamePropertyModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new ShortCircuitSamePropertyModel { Value = "x" }, context, default);

        Assert.Equal("First async error", context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).Single());
        Assert.True(NeverCompletesUnlessCanceledAttribute.Canceled);
    }

    [Fact]
    public async Task AsyncValidation_MultipleAttributesOnSameProperty_RunInParallel()
    {
        ParallelSuccessAttribute.Reset(expectedStarts: 2);
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ParallelSamePropertyModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new ParallelSamePropertyModel { Value = "x" }, context, default);
        await ParallelSuccessAttribute.AllStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        ParallelSuccessAttribute.Gate.SetResult();
        await task;

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Fact]
    public async Task MultipleAsyncAttributesOnSameProperty_FailingConcurrently_RecordAllErrors()
    {
        ConcurrentFailAttribute.Reset(expectedStarts: 2);
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ConcurrentFailSamePropertyModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new ConcurrentFailSamePropertyModel { Value = "x" }, context, default);

        var errors = context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).ToArray();
        Assert.Contains("Concurrent error A", errors);
        Assert.Contains("Concurrent error B", errors);
    }

    [Fact]
    public async Task AsyncValidation_DeepNestedObjects_ValidateInParallel()
    {
        GatedSuccessAttribute.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<DeepParallelRoot>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new DeepParallelRoot { Left = new() { Leaf = new() { Value = "x" } }, Right = new() { Leaf = new() { Value = "y" } } }, context, default);
        await GatedSuccessAttribute.WaitForStartsAsync(2);
        GatedSuccessAttribute.Gate.SetResult();
        await task;

        Assert.True(context.ValidationErrors is null || context.ValidationErrors.Count == 0);
    }

    [Fact]
    public async Task IAsyncValidatableObject_DoesNotRunInParallelWithPropertyValidation()
    {
        PropertyCompletionTrackingAttribute.Reset();
        SequentialAsyncValidatableModel.AsyncStartedAfterPropertyCompleted = false;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<SequentialAsyncValidatableModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new SequentialAsyncValidatableModel { Value = "x" }, context, default);
        await PropertyCompletionTrackingAttribute.WaitForStartsAsync(1);
        Assert.False(SequentialAsyncValidatableModel.AsyncStartedAfterPropertyCompleted);
        PropertyCompletionTrackingAttribute.Gate.SetResult();
        await task;

        Assert.True(SequentialAsyncValidatableModel.AsyncStartedAfterPropertyCompleted);
    }

    [Fact]
    public async Task TypeLevelAttributeValidation_RunsOnlyAfterMemberValidationCompletes()
    {
        PropertyCompletionTrackingAttribute.Reset();
        TypeLevelAfterMembersAttribute.StartedAfterPropertyCompleted = false;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<TypeLevelAfterMembersModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new TypeLevelAfterMembersModel { Value = "x" }, context, default);
        await PropertyCompletionTrackingAttribute.WaitForStartsAsync(1);
        Assert.False(TypeLevelAfterMembersAttribute.StartedAfterPropertyCompleted);
        PropertyCompletionTrackingAttribute.Gate.SetResult();
        await task;

        Assert.True(TypeLevelAfterMembersAttribute.StartedAfterPropertyCompleted);
    }

    [Fact]
    public async Task AsyncValidation_PropertyWithAsyncFailure_ShortCircuitsTypeLevelAttribute()
    {
        TypeLevelShouldNotRunAttribute.CallCount = 0;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<PropertyFailureShortCircuitsTypeLevelModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new PropertyFailureShortCircuitsTypeLevelModel { Value = "x" }, context, default);

        Assert.Equal(0, TypeLevelShouldNotRunAttribute.CallCount);
        Assert.Equal("Property async error", context.ValidationErrors!["Value"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_ShortCircuitsOnPropertyError()
    {
        AsyncValidatableWithRequiredProperty.AsyncCalls = 0;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<AsyncValidatableWithRequiredProperty>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new AsyncValidatableWithRequiredProperty(), context, default);

        Assert.Equal(0, AsyncValidatableWithRequiredProperty.AsyncCalls);
        Assert.Equal("The Name field is required.", context.ValidationErrors!["Name"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task AsyncValidation_OnParameterCollection_AwaitsAsyncValidatorsOnItems()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var parameter = typeof(AsyncParameterActions).GetMethod(nameof(AsyncParameterActions.Users))!.GetParameters()[0];
        Assert.True(options.TryGetValidatableParameterInfo(parameter, out var parameterInfo));
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await parameterInfo.ValidateAsync(new[] { new AsyncUser { Email = "duplicate@example.com" } }, context, default);

        Assert.Equal("Email already exists", context.ValidationErrors!["users[0].Email"].Select(e => e.ErrorMessage).Single());
    }

    [Fact]
    public async Task ValidateMembers_PropertyGetterThrows_ExceptionShouldPropagate()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ThrowingGetterModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var ex = await Assert.ThrowsAsync<TargetInvocationException>(() => typeInfo.ValidateAsync(new ThrowingGetterModel(), context, default));

        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("Getter throws", ex.InnerException.Message);
    }

    [Fact]
    public async Task SiblingSubtreeError_DoesNotSuppress_UnrelatedTypesValidatableObject()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<SiblingSubtreeRoot>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new SiblingSubtreeRoot { Bad = new(), Other = new() { Name = "ok" } }, context, default);

        Assert.Contains("Bad.Name", context.ValidationErrors!.Keys);
        Assert.Contains("Other.Value", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task CollectionItemError_DoesNotSuppress_SiblingItemsValidatableObject()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<CollectionSiblingRoot>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new CollectionSiblingRoot { Items = [new() { Name = null }, new() { Name = "ok", ObjectError = true }] }, context, default);

        Assert.Contains("Items[0].Name", context.ValidationErrors!.Keys);
        Assert.Contains("Items[1].Value", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task FirstMemberSubtree_NotSuppressed_ByLaterSiblingError()
    {
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<FirstSubtreeLaterErrorRoot>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        await typeInfo.ValidateAsync(new FirstSubtreeLaterErrorRoot { First = new() { Name = "ok", ObjectError = true } }, context, default);

        Assert.Contains("First.Value", context.ValidationErrors!.Keys);
        Assert.Contains("Second", context.ValidationErrors.Keys);
    }

    [Fact]
    public async Task MixedSyncAsyncMembers_DoNotReuseAParkedContext()
    {
        GatedSuccessAttribute.Reset();
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<MixedSyncAsyncParkedContextModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new MixedSyncAsyncParkedContextModel { First = "x" }, context, default);
        await GatedSuccessAttribute.WaitForStartsAsync(1);
        GatedSuccessAttribute.Gate.SetResult();
        await task;

        Assert.Contains("Second", context.ValidationErrors!.Keys);
        Assert.Contains("Third", context.ValidationErrors.Keys);
        Assert.DoesNotContain(context.ValidationErrors.Keys, k => k.StartsWith("First.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PropertyErrorOnClonedMember_StillShortCircuitsTypeLevelValidation()
    {
        GatedSuccessAttribute.Reset();
        TypeLevelShouldNotRunAttribute.CallCount = 0;
        var (provider, options) = GeneratedValidationTestHelpers.CreateValidationServices();
        var typeInfo = GeneratedValidationTestHelpers.GetTypeInfo<ClonedMemberErrorShortCircuitsTypeModel>(options);
        var context = GeneratedValidationTestHelpers.CreateContext(provider, options);

        var task = typeInfo.ValidateAsync(new ClonedMemberErrorShortCircuitsTypeModel { First = "x", Second = "y" }, context, default);
        await GatedSuccessAttribute.WaitForStartsAsync(1);
        GatedSuccessAttribute.Gate.SetResult();
        await task;

        Assert.Equal(0, TypeLevelShouldNotRunAttribute.CallCount);
        Assert.Equal("Property async error", context.ValidationErrors!["Second"].Select(e => e.ErrorMessage).Single());
    }
}

[ValidatableType]
public class AsyncUser
{
    [EmailExists]
    public string? Email { get; set; }
}

public sealed class EmailExistsAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return value is string email && email == "duplicate@example.com" ? new ValidationResult("Email already exists") : ValidationResult.Success;
    }
}

[ValidatableType]
public class AsyncProduct
{
    [Required]
    public string? Name { get; set; }
    [SkuExists]
    public string? SKU { get; set; }
}

public sealed class SkuExistsAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return value is string sku && sku.Contains("DUPLICATE", StringComparison.Ordinal) ? new ValidationResult("SKU already exists") : ValidationResult.Success;
    }
}

[ValidatableType]
public class RequiredBeforeAsyncModel
{
    [Required]
    [TrackingAsync]
    public string? Value { get; set; }
}

public sealed class TrackingAsyncAttribute : AsyncValidationAttribute
{
    public static int CallCount { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        CallCount++;
        return ValidationResult.Success;
    }
}

[ValidatableType]
public class AsyncValidatableAccount : IAsyncValidatableObject
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        if (Username == "taken")
        {
            yield return new ValidationResult("Username is already taken", [nameof(Username)]);
        }

        if (Email == "duplicate@example.com")
        {
            yield return new ValidationResult("Email is already registered", [nameof(Email)]);
        }
    }
}

[ValidatableType]
public class AsyncRegistrationForm : IAsyncValidatableObject
{
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        if (Password != ConfirmPassword)
        {
            yield return new ValidationResult("Passwords do not match", [nameof(Password), nameof(ConfirmPassword)]);
        }
    }
}

[ValidatableType]
public class AsyncOrder
{
    public AsyncUser? Customer { get; set; }
}

[ValidatableType]
public class CancellableAsyncModel
{
    [CancelledAsync]
    public string? Value { get; set; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class CancelledAsyncAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        return ValidationResult.Success;
    }
}

[AsyncTypeFails]
[ValidatableType]
public class AsyncTypeLevelModel
{
    public string? Value { get; set; }
}

public sealed class AsyncTypeFailsAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new ValidationResult("Type-level async error");
    }
}

[ValidatableType]
public class AsyncUserCollection
{
    public List<AsyncUser> Users { get; set; } = [];
}

[ValidatableType]
public class ThrowingAsyncModel
{
    [ThrowingAsync]
    public string? Value { get; set; }
}

public sealed class ThrowingAsyncAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Async validation exception");
}

internal static class AsyncTestState
{
    public static TaskCompletionSource CreateTcs()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public abstract class CoordinatedAsyncAttribute : AsyncValidationAttribute
{
    public static TaskCompletionSource Gate { get; set; } = AsyncTestState.CreateTcs();
    public static TaskCompletionSource<int> StartedSignal { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static int StartedCount;
    public static int ExpectedStarts;

    public static void Reset(int expectedStarts = 1)
    {
        Gate = AsyncTestState.CreateTcs();
        StartedSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StartedCount = 0;
        ExpectedStarts = expectedStarts;
    }

    public static Task WaitForStartsAsync(int count)
    {
        ExpectedStarts = count;
        if (Volatile.Read(ref StartedCount) >= count)
        {
            return Task.CompletedTask;
        }

        return StartedSignal.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    protected static void RecordStart()
    {
        var count = Interlocked.Increment(ref StartedCount);
        if (count >= ExpectedStarts)
        {
            StartedSignal.TrySetResult(count);
        }
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();
}

public sealed class GatedSuccessAttribute : CoordinatedAsyncAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        RecordStart();
        await Gate.Task.WaitAsync(cancellationToken);
        return ValidationResult.Success;
    }
}

public sealed class DelayedFailAttribute : CoordinatedAsyncAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        RecordStart();
        await Gate.Task.WaitAsync(cancellationToken);
        return new ValidationResult("Delayed validation failed");
    }
}

public sealed class ImmediateAsyncFailAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new ValidationResult("First async error");
    }
}

public sealed class PropertyAsyncFailAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return new ValidationResult("Property async error");
    }
}

public sealed class NeverCompletesUnlessCanceledAttribute : AsyncValidationAttribute
{
    public static bool Canceled { get; set; }

    public static void Reset() => Canceled = false;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Canceled = true;
            throw;
        }

        return new ValidationResult("Should not complete");
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ParallelSuccessAttribute(string key) : AsyncValidationAttribute
{
    public static TaskCompletionSource Gate { get; set; } = AsyncTestState.CreateTcs();
    public static TaskCompletionSource AllStarted { get; set; } = AsyncTestState.CreateTcs();
    public static int StartedCount;
    public static int ExpectedStarts;

    public static void Reset(int expectedStarts)
    {
        Gate = AsyncTestState.CreateTcs();
        AllStarted = AsyncTestState.CreateTcs();
        StartedCount = 0;
        ExpectedStarts = expectedStarts;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        _ = key;
        if (Interlocked.Increment(ref StartedCount) >= ExpectedStarts)
        {
            AllStarted.TrySetResult();
        }

        await Gate.Task.WaitAsync(cancellationToken);
        return ValidationResult.Success;
    }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ConcurrentFailAttribute(string key) : AsyncValidationAttribute
{
    public static TaskCompletionSource Gate { get; set; } = AsyncTestState.CreateTcs();
    public static TaskCompletionSource AllStarted { get; set; } = AsyncTestState.CreateTcs();
    public static int StartedCount;
    public static int ExpectedStarts;

    public static void Reset(int expectedStarts)
    {
        Gate = AsyncTestState.CreateTcs();
        AllStarted = AsyncTestState.CreateTcs();
        StartedCount = 0;
        ExpectedStarts = expectedStarts;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref StartedCount) >= ExpectedStarts)
        {
            AllStarted.TrySetResult();
        }

        await AllStarted.Task;
        return new ValidationResult($"Concurrent error {key}");
    }
}

public sealed class PropertyCompletionTrackingAttribute : CoordinatedAsyncAttribute
{
    public static bool Completed { get; set; }

    public new static void Reset(int expectedStarts = 1)
    {
        CoordinatedAsyncAttribute.Reset(expectedStarts);
        Completed = false;
    }

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        RecordStart();
        await Gate.Task.WaitAsync(cancellationToken);
        Completed = true;
        return ValidationResult.Success;
    }
}

public sealed class TypeLevelAfterMembersAttribute : AsyncValidationAttribute
{
    public static bool StartedAfterPropertyCompleted { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        StartedAfterPropertyCompleted = PropertyCompletionTrackingAttribute.Completed;
        return Task.FromResult<ValidationResult?>(ValidationResult.Success);
    }
}

public sealed class TypeLevelShouldNotRunAttribute : AsyncValidationAttribute
{
    public static int CallCount { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException();

    protected override Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult<ValidationResult?>(ValidationResult.Success);
    }
}

[ValidatableType]
public class AsyncValidatableWithPropertyError : IAsyncValidatableObject
{
    public static int AsyncCalls { get; set; }
    [EmailExists]
    public string? Email { get; set; }
    public string? Bio { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        AsyncCalls++;
        if (Bio == "short")
        {
            yield return new ValidationResult("Bio is too short", [nameof(Bio)]);
        }
    }
}

[ValidatableType]
public class AsyncProfile : IAsyncValidatableObject
{
    public string? Bio { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        if (Bio == "short")
        {
            yield return new ValidationResult("Bio is too short", [nameof(Bio)]);
        }
    }
}

[ValidatableType]
public class NestedAsyncProfileContainer
{
    public AsyncProfile? Profile { get; set; }
}

[ValidatableType]
public class GatedAsyncProfile : IAsyncValidatableObject
{
    public static TaskCompletionSource Gate { get; set; } = AsyncTestState.CreateTcs();
    public static TaskCompletionSource<int> Started { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public static int StartedCount;
    public string? Bio { get; set; }

    public static void Reset()
    {
        Gate = AsyncTestState.CreateTcs();
        Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StartedCount = 0;
    }

    public static Task WaitForStartsAsync(int count)
        => Volatile.Read(ref StartedCount) >= count ? Task.CompletedTask : Started.Task.WaitAsync(TimeSpan.FromSeconds(10));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref StartedCount) >= 2)
        {
            Started.TrySetResult(StartedCount);
        }

        await Gate.Task.WaitAsync(cancellationToken);
        yield return new ValidationResult("Bio is too short", [nameof(Bio)]);
    }
}

[ValidatableType]
public class TwoNestedAsyncProfiles
{
    public GatedAsyncProfile? First { get; set; }
    public GatedAsyncProfile? Second { get; set; }
}

[ValidatableType]
public class GatedThenRequiredModel
{
    [GatedSuccess]
    public string? First { get; set; }
    [Required]
    public string? Second { get; set; }
}

[ValidatableType]
public class GatedComplexThenRequiredModel
{
    public GatedComplexChild? First { get; set; }
    [Required]
    public string? Second { get; set; }
}

[ValidatableType]
public class GatedComplexChild
{
    [GatedSuccess]
    public string? Value { get; set; }
}

[ValidatableType]
public class MultipleCancellableAsyncModel
{
    [CancelledAsync]
    [CancelledAsync]
    public string? Value { get; set; }
}

[ValidatableType]
public class DelayedValidationModel
{
    [DelayedFail]
    public string? Value { get; set; }
}

[ValidatableType]
public class ShortCircuitSamePropertyModel
{
    [ImmediateAsyncFail]
    [NeverCompletesUnlessCanceled]
    public string? Value { get; set; }
}

[ValidatableType]
public class ParallelSamePropertyModel
{
    [ParallelSuccess("A")]
    [ParallelSuccess("B")]
    public string? Value { get; set; }
}

[ValidatableType]
public class ConcurrentFailSamePropertyModel
{
    [ConcurrentFail("A")]
    [ConcurrentFail("B")]
    public string? Value { get; set; }
}

[ValidatableType]
public class DeepParallelRoot
{
    public DeepParallelBranch? Left { get; set; }
    public DeepParallelBranch? Right { get; set; }
}

[ValidatableType]
public class DeepParallelBranch
{
    public DeepParallelLeaf? Leaf { get; set; }
}

[ValidatableType]
public class DeepParallelLeaf
{
    [GatedSuccess]
    public string? Value { get; set; }
}

[ValidatableType]
public class SequentialAsyncValidatableModel : IAsyncValidatableObject
{
    public static bool AsyncStartedAfterPropertyCompleted { get; set; }
    [PropertyCompletionTracking]
    public string? Value { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        AsyncStartedAfterPropertyCompleted = PropertyCompletionTrackingAttribute.Completed;
        yield break;
    }
}

[TypeLevelAfterMembers]
[ValidatableType]
public class TypeLevelAfterMembersModel
{
    [PropertyCompletionTracking]
    public string? Value { get; set; }
}

[TypeLevelShouldNotRun]
[ValidatableType]
public class PropertyFailureShortCircuitsTypeLevelModel
{
    [PropertyAsyncFail]
    public string? Value { get; set; }
}

[ValidatableType]
public class AsyncValidatableWithRequiredProperty : IAsyncValidatableObject
{
    public static int AsyncCalls { get; set; }
    [Required]
    public string? Name { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => throw new NotImplementedException();
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(ValidationContext validationContext, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        AsyncCalls++;
        yield break;
    }
}

public static class AsyncParameterActions
{
    public static void Users([Required] IEnumerable<AsyncUser> users) { }
}

[ValidatableType]
public class ThrowingGetterModel
{
    [Required]
    public string Throwing => throw new InvalidOperationException("Getter throws");
}

[ValidatableType]
public class SiblingSubtreeRoot
{
    public SiblingBadChild? Bad { get; set; }
    public ObjectErrorChild? Other { get; set; }
}

[ValidatableType]
public class SiblingBadChild
{
    [Required]
    public string? Name { get; set; }
}

[ValidatableType]
public class ObjectErrorChild : IValidatableObject
{
    [Required]
    public string? Name { get; set; }
    public bool ObjectError { get; set; } = true;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ObjectError)
        {
            yield return new ValidationResult("Object error", ["Value"]);
        }
    }
}

[ValidatableType]
public class CollectionSiblingRoot
{
    public List<ObjectErrorChild> Items { get; set; } = [];
}

[ValidatableType]
public class FirstSubtreeLaterErrorRoot
{
    public ObjectErrorChild? First { get; set; }
    [Required]
    public string? Second { get; set; }
}

[ValidatableType]
public class MixedSyncAsyncParkedContextModel
{
    [GatedSuccess]
    public string? First { get; set; }
    [Required]
    public string? Second { get; set; }
    [Required]
    public string? Third { get; set; }
}

[TypeLevelShouldNotRun]
[ValidatableType]
public class ClonedMemberErrorShortCircuitsTypeModel
{
    [GatedSuccess]
    public string? First { get; set; }
    [PropertyAsyncFail]
    public string? Second { get; set; }
}

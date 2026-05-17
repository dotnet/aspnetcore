// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace Microsoft.AspNetCore.Components.Forms;

public class EditContextValidatableInfoFieldTest
{
    [Fact]
    public Task NotifyFieldChanged_ValidValue_ProducesNoMessages() => RunOnDispatcher(async () =>
    {
        var model = new RootModel { Name = "valid" };
        var (editContext, _) = CreateEditContextWithValidation(model);
        var field = new FieldIdentifier(model, nameof(RootModel.Name));

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.False(editContext.IsValidationFaulted());
        Assert.Empty(editContext.GetValidationMessages());
    });

    [Fact]
    public Task NotifyFieldChanged_InvalidValue_AddsMessageToFieldIdentifier() => RunOnDispatcher(async () =>
    {
        var model = new RootModel { Name = null };
        var (editContext, _) = CreateEditContextWithValidation(model);
        var field = new FieldIdentifier(model, nameof(RootModel.Name));

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.Equal(new[] { "Name is required." }, editContext.GetValidationMessages(field));
        Assert.Equal(new[] { "Name is required." }, editContext.GetValidationMessages());
    });

    [Fact]
    public Task NotifyFieldChanged_OnListItemValueProperty_AddsMessageToItemFieldIdentifier() => RunOnDispatcher(async () =>
    {
        // Mirrors the typical Razor pattern:
        //   @foreach (var item in model.Items) {
        //       <InputNumber       @bind-Value="item.Value" />
        //       <ValidationMessage For="@(() => item.Value)" />
        //   }
        // Both InputNumber and ValidationMessage produce FieldIdentifier(item, "Value"),
        // so the per-field validator must attribute the error to that same identifier.
        var item0 = new ItemModel { Value = 50 };  // valid
        var item1 = new ItemModel { Value = 200 }; // out of range
        var model = new RootModel { Name = "valid", Items = { item0, item1 } };
        var (editContext, _) = CreateEditContextWithValidation(model);

        var item1Field = new FieldIdentifier(item1, nameof(ItemModel.Value));
        editContext.NotifyFieldChanged(item1Field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(item1Field));

        Assert.Equal(new[] { "Value out of range." }, editContext.GetValidationMessages(item1Field));

        // The error must NOT be misattributed to the parent collection field, otherwise
        // <ValidationMessage For="@(() => item1.Value)" /> would not display it.
        var parentItemsField = new FieldIdentifier(model, nameof(RootModel.Items));
        Assert.Empty(editContext.GetValidationMessages(parentItemsField));

        // The other (valid) item must remain untouched.
        var item0Field = new FieldIdentifier(item0, nameof(ItemModel.Value));
        Assert.Empty(editContext.GetValidationMessages(item0Field));
    });

    [Fact]
    public Task NotifyFieldChanged_FixingValue_ClearsPreviousMessage() => RunOnDispatcher(async () =>
    {
        var item = new ItemModel { Value = 200 };
        var model = new RootModel { Name = "valid", Items = { item } };
        var (editContext, _) = CreateEditContextWithValidation(model);
        var field = new FieldIdentifier(item, nameof(ItemModel.Value));

        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));
        Assert.NotEmpty(editContext.GetValidationMessages(field));

        item.Value = 50;
        editContext.NotifyFieldChanged(field);
        await WaitUntilAsync(() => !editContext.IsValidationPending(field));

        Assert.Empty(editContext.GetValidationMessages(field));
        Assert.Empty(editContext.GetValidationMessages());
    });

    [Fact]
    public void NotifyFieldChanged_PropertyNotInValidationOptions_DoesNothing()
    {
        var model = new RootModel { Name = "valid" };
        var (editContext, _) = CreateEditContextWithValidation(model);
        var field = new FieldIdentifier(model, "DoesNotExist");
        var notificationCount = 0;
        editContext.OnValidationStateChanged += (_, _) => notificationCount++;

        editContext.NotifyFieldChanged(field);

        Assert.False(editContext.IsValidationPending());
        Assert.False(editContext.IsValidationPending(field));
        Assert.Empty(editContext.GetValidationMessages());
        Assert.Equal(0, notificationCount);
    }

    [Fact]
    public Task NotifyFieldChanged_DoesNotValidateSiblingFields() => RunOnDispatcher(async () =>
    {
        // Per-field validation must only inspect the single field that changed,
        // even when the same model has other invalid properties.
        var item = new ItemModel { Value = 200 };  // would fail Range
        var model = new RootModel { Name = null, Items = { item } };  // would fail Required
        var (editContext, _) = CreateEditContextWithValidation(model);
        var nameField = new FieldIdentifier(model, nameof(RootModel.Name));

        editContext.NotifyFieldChanged(nameField);
        await WaitUntilAsync(() => !editContext.IsValidationPending(nameField));

        // Name is invalid -> message present.
        Assert.Equal(new[] { "Name is required." }, editContext.GetValidationMessages(nameField));

        // The unrelated invalid item value must not have been touched.
        var itemField = new FieldIdentifier(item, nameof(ItemModel.Value));
        Assert.Empty(editContext.GetValidationMessages(itemField));
    });

    private static (EditContext editContext, IDisposable subscription) CreateEditContextWithValidation(RootModel model)
    {
        var rootTypeInfo = new TestValidatableTypeInfo(typeof(RootModel),
        [
            new TestValidatablePropertyInfo(typeof(RootModel), typeof(string), nameof(RootModel.Name), "Name",
                [new RequiredAttribute { ErrorMessage = "Name is required." }]),
            new TestValidatablePropertyInfo(typeof(RootModel), typeof(List<ItemModel>), nameof(RootModel.Items), "Items",
                []),
        ]);
        var itemTypeInfo = new TestValidatableTypeInfo(typeof(ItemModel),
        [
            new TestValidatablePropertyInfo(typeof(ItemModel), typeof(int), nameof(ItemModel.Value), "Value",
                [new RangeAttribute(1, 100) { ErrorMessage = "Value out of range." }]),
        ]);

        var options = new TestValidationOptions(new Dictionary<Type, ValidatableTypeInfo>
        {
            [typeof(RootModel)] = rootTypeInfo,
            [typeof(ItemModel)] = itemTypeInfo,
        });

        var serviceProvider = new TestServiceProvider();
        serviceProvider.AddService<IOptions<ValidationOptions>>(Options.Create<ValidationOptions>(options));

        var editContext = new EditContext(model);
        var subscription = editContext.EnableDataAnnotationsValidation(serviceProvider);
        return (editContext, subscription);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The expected condition was not reached before the timeout.");
            }

            await Task.Yield();
        }
    }

    // Runs a test body under Blazor's default dispatcher to simulate the renderer threading model:
    // validator continuations, the framework's ObserveValidationTaskAsync continuation, and the test
    // body's polling/assertions all serialize through a single logical thread. This matches
    // EditContext's documented threading model (see EditContext class XML remarks) and removes
    // incidental cross-thread races that would otherwise occur when raw EditContext is exercised on
    // the bare thread pool. Uses the public Dispatcher.CreateDefault() — the same factory the
    // renderer uses in production — consistent with precedent across the Blazor test suite.
    private static Task RunOnDispatcher(Func<Task> body)
        => Dispatcher.CreateDefault().InvokeAsync(body);

    private sealed class RootModel
    {
        public string? Name { get; set; }

        public List<ItemModel> Items { get; } = new();
    }

    private sealed class ItemModel
    {
        public int Value { get; set; } = 1;
    }

    private sealed class TestValidatableTypeInfo : ValidatableTypeInfo
    {
        public TestValidatableTypeInfo(Type type, IReadOnlyList<ValidatablePropertyInfo> members)
            : base(type, members)
        {
        }

        protected override ValidationAttribute[] GetValidationAttributes() => [];
    }

    private sealed class TestValidatablePropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _attributes;

        public TestValidatablePropertyInfo(
            Type declaringType,
            Type propertyType,
            string name,
            string displayName,
            ValidationAttribute[] attributes)
            : base(declaringType, propertyType, name, displayName)
        {
            _attributes = attributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }

    private sealed class TestValidationOptions : ValidationOptions
    {
        public TestValidationOptions(Dictionary<Type, ValidatableTypeInfo> mappings)
        {
            Resolvers.Add(new DictionaryResolver(mappings));
        }

        private sealed class DictionaryResolver : IValidatableInfoResolver
        {
            private readonly Dictionary<Type, ValidatableTypeInfo> _mappings;

            public DictionaryResolver(Dictionary<Type, ValidatableTypeInfo> mappings)
            {
                _mappings = mappings;
            }

            public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                if (_mappings.TryGetValue(type, out var info))
                {
                    validatableInfo = info;
                    return true;
                }

                validatableInfo = null;
                return false;
            }

            public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableInfo? validatableInfo)
            {
                validatableInfo = null;
                return false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

namespace Microsoft.AspNetCore.Components.Infrastructure;

internal static class BuiltInComponentDescriptors
{
    internal static ComponentDescriptor[] GetDescriptors()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Web.EnvironmentView),
                Injectables =
                [
                    CreateInjectable<Web.EnvironmentView, IHostEnvironment>(
                        "HostEnvironment",
                        SetHostEnvironment),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(Web.ErrorBoundary),
                Injectables =
                [
                    CreateInjectable<Web.ErrorBoundary, Web.IErrorBoundaryLogger?>(
                        "ErrorBoundaryLogger",
                        SetErrorBoundaryLogger),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(Web.HeadOutlet),
                Injectables =
                [
                    CreateInjectable<Web.HeadOutlet, IJSRuntime>(
                        "JSRuntime",
                        SetHeadOutletJSRuntime),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(FocusOnNavigate),
                Injectables =
                [
                    CreateInjectable<FocusOnNavigate, IJSRuntime>(
                        "JSRuntime",
                        SetFocusOnNavigateJSRuntime),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(NavigationLock),
                Injectables =
                [
                    CreateInjectable<NavigationLock, IJSRuntime>(
                        "JSRuntime",
                        SetNavigationLockJSRuntime),
                    CreateInjectable<NavigationLock, NavigationManager>(
                        "NavigationManager",
                        SetNavigationLockNavigationManager),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(NavLink),
                Injectables =
                [
                    CreateInjectable<NavLink, NavigationManager>(
                        nameof(NavLink.NavigationManager),
                        static (target, value) => target.NavigationManager = value),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(AntiforgeryToken),
                Injectables =
                [
                    CreateInjectable<AntiforgeryToken, IServiceProvider>(
                        "Services",
                        SetAntiforgeryTokenServices),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(EditForm),
                Parameters =
                [
                    CreateCascadingParameter<EditForm, FormMappingContext?>(
                        "MappingContext",
                        GetEditFormMappingContext,
                        SetEditFormMappingContext),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(FormMappingScope),
                Injectables =
                [
                    CreateInjectable<FormMappingScope, IFormValueMapper?>(
                        nameof(FormMappingScope.FormValueModelBinder),
                        static (target, value) => target.FormValueModelBinder = value),
                ],
            },
            CreateInputBaseDescriptor<InputCheckbox, bool>(),
            CreateInputBaseDescriptor<InputHidden, string?>(),
            CreateInputBaseDescriptor<InputText, string?>(),
            CreateInputBaseDescriptor<InputTextArea, string?>(),
            new ComponentDescriptor
            {
                Type = typeof(InputFile),
                Injectables =
                [
                    CreateInjectable<InputFile, IJSRuntime>(
                        nameof(InputFile.JSRuntime),
                        static (target, value) => target.JSRuntime = value),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(ValidationSummary),
                Parameters =
                [
                    CreateCascadingParameter<ValidationSummary, EditContext>(
                        "CurrentEditContext",
                        GetValidationSummaryCurrentEditContext,
                        SetValidationSummaryCurrentEditContext),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(FormMappingValidator),
                CreateInstance = static _ => new FormMappingValidator(),
                Parameters =
                [
                    CreateParameter<FormMappingValidator, EditContext?>(
                        nameof(FormMappingValidator.CurrentEditContext),
                        static target => target.CurrentEditContext,
                        static (target, value) => target.CurrentEditContext = value),
                    CreateCascadingParameter<FormMappingValidator, FormMappingContext?>(
                        nameof(FormMappingValidator.MappingContext),
                        static target => target.MappingContext,
                        static (target, value) => target.MappingContext = value),
                ],
            },
            new ComponentDescriptor
            {
                Type = typeof(ClientValidationData),
                CreateInstance = static _ => new ClientValidationData(),
                Parameters =
                [
                    new ComponentParameterDescriptor
                    {
                        Name = "CurrentEditContext",
                        ParameterType = typeof(EditContext),
                        Attribute = new CascadingParameterAttribute(),
                        GetValue = static target => GetCurrentEditContext((ClientValidationData)target),
                        SetValue = static (target, value) =>
                            SetCurrentEditContext((ClientValidationData)target, (EditContext?)value),
                    },
                ],
                Injectables =
                [
                    new ComponentInjectableDescriptor
                    {
                        Name = "Services",
                        ServiceType = typeof(IServiceProvider),
                        Attribute = new InjectAttribute(),
                        SetValue = static (target, value) =>
                            SetServices((ClientValidationData)target, (IServiceProvider)value!),
                    },
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateInputDateDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        => [CreateInputBaseDescriptor<InputDate<TValue>, TValue>()];

    internal static ComponentDescriptor[] CreateInputNumberDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        => [CreateInputBaseDescriptor<InputNumber<TValue>, TValue>()];

    internal static ComponentDescriptor[] CreateInputRadioDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(InputRadio<TValue>),
                Parameters =
                [
                    CreateCascadingParameter<InputRadio<TValue>, InputRadioContext?>(
                        "CascadedContext",
                        InputRadioAccessors<TValue>.GetCascadedContext,
                        InputRadioAccessors<TValue>.SetCascadedContext),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateInputRadioGroupDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(InputRadioGroup<TValue>),
                Parameters =
                [
                    .. CreateInputBaseParameters<TValue>(),
                    CreateCascadingParameter<InputRadioGroup<TValue>, InputRadioContext?>(
                        "CascadedContext",
                        InputRadioGroupAccessors<TValue>.GetCascadedContext,
                        InputRadioGroupAccessors<TValue>.SetCascadedContext),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateInputSelectDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>()
        => [CreateInputBaseDescriptor<InputSelect<TValue>, TValue>()];

    internal static ComponentDescriptor[] CreateLabelDescriptors<TValue>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Label<TValue>),
                Parameters =
                [
                    CreateCascadingParameter<Label<TValue>, HtmlFieldPrefix>(
                        "FieldPrefix",
                        LabelAccessors<TValue>.GetFieldPrefix,
                        LabelAccessors<TValue>.SetFieldPrefix),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateInputBaseDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInput,
        TValue>()
        where TInput : InputBase<TValue>
        => [CreateInputBaseDescriptor<TInput, TValue>()];

    internal static ComponentDescriptor[] CreateEditorDescriptors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEditor,
        TValue>()
        where TEditor : Editor<TValue>
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(TEditor),
                Parameters =
                [
                    CreateCascadingParameter<Editor<TValue>, HtmlFieldPrefix>(
                        "FieldPrefix",
                        EditorAccessors<TValue>.GetFieldPrefix,
                        EditorAccessors<TValue>.SetFieldPrefix),
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateValidationMessageDescriptors<TValue>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(ValidationMessage<TValue>),
                CreateInstance = static _ => new ValidationMessage<TValue>(),
                Parameters =
                [
                    new ComponentParameterDescriptor
                    {
                        Name = nameof(ValidationMessage<TValue>.AdditionalAttributes),
                        ParameterType = typeof(IReadOnlyDictionary<string, object>),
                        Attribute = new ParameterAttribute { CaptureUnmatchedValues = true },
                        GetValue = static target => ((ValidationMessage<TValue>)target).AdditionalAttributes,
                        SetValue = static (target, value) =>
                            ((ValidationMessage<TValue>)target).AdditionalAttributes =
                                (IReadOnlyDictionary<string, object>?)value,
                    },
                    new ComponentParameterDescriptor
                    {
                        Name = "CurrentEditContext",
                        ParameterType = typeof(EditContext),
                        Attribute = new CascadingParameterAttribute(),
                        GetValue = static target =>
                            ValidationMessageAccessors<TValue>.GetCurrentEditContext((ValidationMessage<TValue>)target),
                        SetValue = static (target, value) =>
                            ValidationMessageAccessors<TValue>.SetCurrentEditContext(
                                (ValidationMessage<TValue>)target,
                                (EditContext)value!),
                    },
                    new ComponentParameterDescriptor
                    {
                        Name = "FieldPrefix",
                        ParameterType = typeof(HtmlFieldPrefix),
                        Attribute = new CascadingParameterAttribute(),
                        GetValue = static target =>
                            ValidationMessageAccessors<TValue>.GetFieldPrefix((ValidationMessage<TValue>)target),
                        SetValue = static (target, value) =>
                            ValidationMessageAccessors<TValue>.SetFieldPrefix(
                                (ValidationMessage<TValue>)target,
                                (HtmlFieldPrefix?)value),
                    },
                    new ComponentParameterDescriptor
                    {
                        Name = nameof(ValidationMessage<TValue>.For),
                        ParameterType = typeof(Expression<Func<TValue>>),
                        Attribute = new ParameterAttribute(),
                        GetValue = static target => ((ValidationMessage<TValue>)target).For,
                        SetValue = static (target, value) =>
                            ((ValidationMessage<TValue>)target).For = (Expression<Func<TValue>>?)value,
                    },
                ],
            },
        ];

    internal static ComponentDescriptor[] CreateVirtualizeDescriptors<TItem>()
        =>
        [
            new ComponentDescriptor
            {
                Type = typeof(Virtualize<TItem>),
                CreateInstance = static _ => new Virtualize<TItem>(),
                Parameters =
                [
                    CreateVirtualizeParameter<TItem, RenderFragment<TItem>?>(
                        nameof(Virtualize<TItem>.ChildContent),
                        static target => target.ChildContent,
                        static (target, value) => target.ChildContent = value),
                    CreateVirtualizeParameter<TItem, RenderFragment<TItem>?>(
                        nameof(Virtualize<TItem>.ItemContent),
                        static target => target.ItemContent,
                        static (target, value) => target.ItemContent = value),
                    CreateVirtualizeParameter<TItem, RenderFragment<PlaceholderContext>?>(
                        nameof(Virtualize<TItem>.Placeholder),
                        static target => target.Placeholder,
                        static (target, value) => target.Placeholder = value),
                    CreateVirtualizeParameter<TItem, RenderFragment?>(
                        nameof(Virtualize<TItem>.EmptyContent),
                        static target => target.EmptyContent,
                        static (target, value) => target.EmptyContent = value),
                    CreateVirtualizeParameter<TItem, float>(
                        nameof(Virtualize<TItem>.ItemSize),
                        static target => target.ItemSize,
                        static (target, value) => target.ItemSize = value),
                    CreateVirtualizeParameter<TItem, ItemsProviderDelegate<TItem>?>(
                        nameof(Virtualize<TItem>.ItemsProvider),
                        static target => target.ItemsProvider,
                        static (target, value) => target.ItemsProvider = value),
                    CreateVirtualizeParameter<TItem, ICollection<TItem>?>(
                        nameof(Virtualize<TItem>.Items),
                        static target => target.Items,
                        static (target, value) => target.Items = value),
                    CreateVirtualizeParameter<TItem, int>(
                        nameof(Virtualize<TItem>.OverscanCount),
                        static target => target.OverscanCount,
                        static (target, value) => target.OverscanCount = value),
                    CreateVirtualizeParameter<TItem, string>(
                        nameof(Virtualize<TItem>.SpacerElement),
                        static target => target.SpacerElement,
                        static (target, value) => target.SpacerElement = value),
                    CreateVirtualizeParameter<TItem, int>(
                        nameof(Virtualize<TItem>.MaxItemCount),
                        static target => target.MaxItemCount,
                        static (target, value) => target.MaxItemCount = value),
                    CreateVirtualizeParameter<TItem, VirtualizeAnchorMode>(
                        nameof(Virtualize<TItem>.AnchorMode),
                        static target => target.AnchorMode,
                        static (target, value) => target.AnchorMode = value),
                    CreateVirtualizeParameter<TItem, IEqualityComparer<TItem>>(
                        nameof(Virtualize<TItem>.ItemComparer),
                        static target => target.ItemComparer,
                        static (target, value) => target.ItemComparer = value),
                    CreateVirtualizeParameter<TItem, int>(
                        nameof(Virtualize<TItem>.InitialItemIndex),
                        static target => target.InitialItemIndex,
                        static (target, value) => target.InitialItemIndex = value),
                ],
                Injectables =
                [
                    new ComponentInjectableDescriptor
                    {
                        Name = "JSRuntime",
                        ServiceType = typeof(IJSRuntime),
                        Attribute = new InjectAttribute(),
                        SetValue = static (target, value) =>
                            VirtualizeAccessors<TItem>.SetJSRuntime((Virtualize<TItem>)target, (IJSRuntime)value!),
                    },
                ],
            },
        ];

    private static ComponentDescriptor CreateInputBaseDescriptor<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TInput,
        TValue>()
        where TInput : InputBase<TValue>
        => new()
        {
            Type = typeof(TInput),
            Parameters = CreateInputBaseParameters<TValue>(),
        };

    private static ComponentParameterDescriptor[] CreateInputBaseParameters<TValue>()
        =>
        [
            CreateCascadingParameter<InputBase<TValue>, EditContext?>(
                "CascadedEditContext",
                InputBaseAccessors<TValue>.GetCascadedEditContext,
                InputBaseAccessors<TValue>.SetCascadedEditContext),
            CreateCascadingParameter<InputBase<TValue>, HtmlFieldPrefix>(
                "FieldPrefix",
                InputBaseAccessors<TValue>.GetFieldPrefix,
                InputBaseAccessors<TValue>.SetFieldPrefix),
        ];

    private static ComponentParameterDescriptor CreateParameter<TComponent, TValue>(
        string name,
        Func<TComponent, TValue> getValue,
        Action<TComponent, TValue> setValue)
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new ParameterAttribute(),
            GetValue = target => getValue((TComponent)target),
            SetValue = (target, value) => setValue(
                (TComponent)target,
                value is null ? default! : (TValue)value),
        };

    private static ComponentParameterDescriptor CreateCascadingParameter<TComponent, TValue>(
        string name,
        Func<TComponent, TValue> getValue,
        Action<TComponent, TValue> setValue)
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new CascadingParameterAttribute(),
            GetValue = target => getValue((TComponent)target),
            SetValue = (target, value) => setValue(
                (TComponent)target,
                value is null ? default! : (TValue)value),
        };

    private static ComponentInjectableDescriptor CreateInjectable<TComponent, TValue>(
        string name,
        Action<TComponent, TValue> setValue)
        => new()
        {
            Name = name,
            ServiceType = typeof(TValue),
            Attribute = new InjectAttribute(),
            SetValue = (target, value) => setValue(
                (TComponent)target,
                value is null ? default! : (TValue)value),
        };

    private static ComponentParameterDescriptor CreateVirtualizeParameter<TItem, TValue>(
        string name,
        Func<Virtualize<TItem>, TValue> getValue,
        Action<Virtualize<TItem>, TValue> setValue)
        => new()
        {
            Name = name,
            ParameterType = typeof(TValue),
            Attribute = new ParameterAttribute(),
            GetValue = target => getValue((Virtualize<TItem>)target),
            SetValue = (target, value) => setValue(
                (Virtualize<TItem>)target,
                value is null ? default! : (TValue)value),
        };

    private static class InputBaseAccessors<TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CascadedEditContext")]
        internal static extern EditContext? GetCascadedEditContext(InputBase<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CascadedEditContext")]
        internal static extern void SetCascadedEditContext(InputBase<TValue> target, EditContext? value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_FieldPrefix")]
        internal static extern HtmlFieldPrefix GetFieldPrefix(InputBase<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_FieldPrefix")]
        internal static extern void SetFieldPrefix(InputBase<TValue> target, HtmlFieldPrefix value);
    }

    private static class EditorAccessors<TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_FieldPrefix")]
        internal static extern HtmlFieldPrefix GetFieldPrefix(Editor<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_FieldPrefix")]
        internal static extern void SetFieldPrefix(Editor<TValue> target, HtmlFieldPrefix value);
    }

    private static class InputRadioAccessors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CascadedContext")]
        internal static extern InputRadioContext? GetCascadedContext(InputRadio<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CascadedContext")]
        internal static extern void SetCascadedContext(InputRadio<TValue> target, InputRadioContext? value);
    }

    private static class InputRadioGroupAccessors<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CascadedContext")]
        internal static extern InputRadioContext? GetCascadedContext(InputRadioGroup<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CascadedContext")]
        internal static extern void SetCascadedContext(InputRadioGroup<TValue> target, InputRadioContext? value);
    }

    private static class LabelAccessors<TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_FieldPrefix")]
        internal static extern HtmlFieldPrefix GetFieldPrefix(Label<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_FieldPrefix")]
        internal static extern void SetFieldPrefix(Label<TValue> target, HtmlFieldPrefix value);
    }

    private static class ValidationMessageAccessors<TValue>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CurrentEditContext")]
        internal static extern EditContext GetCurrentEditContext(ValidationMessage<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CurrentEditContext")]
        internal static extern void SetCurrentEditContext(ValidationMessage<TValue> target, EditContext value);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_FieldPrefix")]
        internal static extern HtmlFieldPrefix? GetFieldPrefix(ValidationMessage<TValue> target);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_FieldPrefix")]
        internal static extern void SetFieldPrefix(ValidationMessage<TValue> target, HtmlFieldPrefix? value);
    }

    private static class VirtualizeAccessors<TItem>
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JSRuntime")]
        internal static extern void SetJSRuntime(Virtualize<TItem> target, IJSRuntime value);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CurrentEditContext")]
    private static extern EditContext? GetCurrentEditContext(ClientValidationData target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CurrentEditContext")]
    private static extern void SetCurrentEditContext(ClientValidationData target, EditContext? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Services")]
    private static extern void SetServices(ClientValidationData target, IServiceProvider value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_HostEnvironment")]
    private static extern void SetHostEnvironment(Web.EnvironmentView target, IHostEnvironment value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ErrorBoundaryLogger")]
    private static extern void SetErrorBoundaryLogger(Web.ErrorBoundary target, Web.IErrorBoundaryLogger? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JSRuntime")]
    private static extern void SetHeadOutletJSRuntime(Web.HeadOutlet target, IJSRuntime value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JSRuntime")]
    private static extern void SetFocusOnNavigateJSRuntime(FocusOnNavigate target, IJSRuntime value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_JSRuntime")]
    private static extern void SetNavigationLockJSRuntime(NavigationLock target, IJSRuntime value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_NavigationManager")]
    private static extern void SetNavigationLockNavigationManager(NavigationLock target, NavigationManager value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Services")]
    private static extern void SetAntiforgeryTokenServices(AntiforgeryToken target, IServiceProvider value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MappingContext")]
    private static extern FormMappingContext? GetEditFormMappingContext(EditForm target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MappingContext")]
    private static extern void SetEditFormMappingContext(EditForm target, FormMappingContext? value);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CurrentEditContext")]
    private static extern EditContext GetValidationSummaryCurrentEditContext(ValidationSummary target);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CurrentEditContext")]
    private static extern void SetValidationSummaryCurrentEditContext(ValidationSummary target, EditContext value);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// A <see cref="HtmlHelper"/> for a specific model type.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
public class HtmlHelper<TModel> : HtmlHelper, IHtmlHelper<TModel>
{
    private readonly ModelExpressionProvider _modelExpressionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HtmlHelper{TModel}"/> class.
    /// </summary>
    public HtmlHelper(
        IHtmlGenerator htmlGenerator,
        ICompositeViewEngine viewEngine,
        IModelMetadataProvider metadataProvider,
        IViewBufferScope bufferScope,
        HtmlEncoder htmlEncoder,
        UrlEncoder urlEncoder,
        ModelExpressionProvider modelExpressionProvider)
        : base(
              htmlGenerator,
              viewEngine,
              metadataProvider,
              bufferScope,
              htmlEncoder,
              urlEncoder)
    {
        _modelExpressionProvider = modelExpressionProvider ?? throw new ArgumentNullException(nameof(modelExpressionProvider));
    }

    /// <inheritdoc />
    public new ViewDataDictionary<TModel> ViewData { get; private set; }

    /// <inheritdoc />
    public override void Contextualize(ViewContext viewContext)
    {
        ArgumentNullException.ThrowIfNull(viewContext);

        if (viewContext.ViewData == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    nameof(ViewContext.ViewData),
                    typeof(ViewContext)),
                nameof(viewContext));
        }

        ViewData = viewContext.ViewData as ViewDataDictionary<TModel>;

        if (ViewData == null)
        {
            // The view data that we have at this point might be of a more derived type than the one defined at compile time.
            // For example ViewDataDictionary<Derived> where our TModel is Base and Derived : Base.
            // This can't happen for regular views, but it can happen in razor pages if someone modified the model type through
            // the page application model.
            // In that case, we check if the type of the current view data, 'ViewDataDictionary<TRuntime>' is "covariant" with the
            // one defined at compile time 'ViewDataDictionary<TCompile>'
            var runtimeType = viewContext.ViewData.ModelMetadata.ModelType;
            if (runtimeType != null && typeof(TModel) != runtimeType && typeof(TModel).IsAssignableFrom(runtimeType))
            {
                ViewData = new ViewDataDictionary<TModel>(viewContext.ViewData, viewContext.ViewData.Model);
            }
        }

        if (ViewData == null)
        {
            // viewContext may contain a base ViewDataDictionary instance. So complain about that type, not TModel.
            throw new ArgumentException(Resources.FormatArgumentPropertyUnexpectedType(
                    nameof(ViewContext.ViewData),
                    viewContext.ViewData.GetType().FullName,
                    typeof(ViewDataDictionary<TModel>).FullName),
                nameof(viewContext));
        }

        base.Contextualize(viewContext);
    }

    /// <inheritdoc />
    public IHtmlContent CheckBoxFor(
        Expression<Func<TModel, bool>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateCheckBox(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            isChecked: null,
            htmlAttributes: htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent DropDownListFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        IEnumerable<SelectListItem> selectList,
        string optionLabel,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateDropDown(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            selectList,
            optionLabel,
            htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent DisplayFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        string htmlFieldName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateDisplay(
            modelExpression.ModelExplorer,
            htmlFieldName ?? modelExpression.Name,
            templateName,
            additionalViewData);
    }

    /// <inheritdoc />
    public string DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateDisplayName(modelExpression.ModelExplorer, modelExpression.Name);
    }

    /// <inheritdoc />
    public string DisplayNameForInnerType<TModelItem, TResult>(
        Expression<Func<TModelItem, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = _modelExpressionProvider.CreateModelExpression(
            new ViewDataDictionary<TModelItem>(ViewData, model: null),
            expression);

        return GenerateDisplayName(modelExpression.ModelExplorer, modelExpression.Name);
    }

    /// <inheritdoc />
    public string DisplayTextFor<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return GenerateDisplayText(GetModelExplorer(expression));
    }

    /// <inheritdoc />
    public IHtmlContent EditorFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string templateName,
        string htmlFieldName,
        object additionalViewData)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateEditor(
            modelExpression.ModelExplorer,
            htmlFieldName ?? modelExpression.Name,
            templateName,
            additionalViewData);
    }

    /// <inheritdoc />
    public IHtmlContent HiddenFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateHidden(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            modelExpression.Model,
            useViewData: false,
            htmlAttributes: htmlAttributes);
    }

    /// <inheritdoc />
    public string IdFor<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return GenerateId(GetExpressionName(expression));
    }

    /// <inheritdoc />
    public IHtmlContent LabelFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string labelText,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateLabel(modelExpression.ModelExplorer, modelExpression.Name, labelText, htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent ListBoxFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        IEnumerable<SelectListItem> selectList,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        var name = modelExpression.Name;

        return GenerateListBox(modelExpression.ModelExplorer, name, selectList, htmlAttributes);
    }

    /// <inheritdoc />
    public string NameFor<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var expressionName = GetExpressionName(expression);
        return Name(expressionName);
    }

    /// <inheritdoc />
    public IHtmlContent PasswordFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GeneratePassword(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            value: null,
            htmlAttributes: htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent RadioButtonFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        object value,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(value);

        var modelExpression = GetModelExpression(expression);
        return GenerateRadioButton(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            value,
            isChecked: null,
            htmlAttributes: htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent TextAreaFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        int rows,
        int columns,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateTextArea(modelExpression.ModelExplorer, modelExpression.Name, rows, columns, htmlAttributes);
    }

    /// <inheritdoc />
    public IHtmlContent TextBoxFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string format,
        object htmlAttributes)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateTextBox(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            modelExpression.Model,
            format,
            htmlAttributes);
    }

    private ModelExpression GetModelExpression<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        return _modelExpressionProvider.CreateModelExpression(ViewData, expression);
    }

    /// <summary>
    /// Gets the name for <paramref name="expression"/>.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <returns>The expression name.</returns>
    protected string GetExpressionName<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return _modelExpressionProvider.GetExpressionText(expression);
    }

    /// <summary>
    /// Gets the <see cref="ModelExplorer"/> for <paramref name="expression"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="expression">The expression.</param>
    /// <returns>The <see cref="ModelExplorer"/>.</returns>
    protected ModelExplorer GetModelExplorer<TResult>(Expression<Func<TModel, TResult>> expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return modelExpression.ModelExplorer;
    }

    /// <inheritdoc />
    public IHtmlContent ValidationMessageFor<TResult>(
        Expression<Func<TModel, TResult>> expression,
        string message,
        object htmlAttributes,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateValidationMessage(
            modelExpression.ModelExplorer,
            modelExpression.Name,
            message,
            tag,
            htmlAttributes);
    }

    /// <inheritdoc />
    public string ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string format)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var modelExpression = GetModelExpression(expression);
        return GenerateValue(modelExpression.Name, modelExpression.Model, format, useViewData: false);
    }
}

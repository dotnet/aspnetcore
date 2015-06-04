// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.Extensions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class HtmlHelper<TModel> : HtmlHelper, IHtmlHelper<TModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlHelper{TModel}"/> class.
        /// </summary>
        public HtmlHelper(
            [NotNull] IHtmlGenerator htmlGenerator,
            [NotNull] ICompositeViewEngine viewEngine,
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] IHtmlEncoder htmlEncoder,
            [NotNull] IUrlEncoder urlEncoder,
            [NotNull] IJavaScriptStringEncoder javaScriptStringEncoder)
            : base(htmlGenerator, viewEngine, metadataProvider, htmlEncoder, urlEncoder, javaScriptStringEncoder)
        {
        }

        /// <inheritdoc />
        public new ViewDataDictionary<TModel> ViewData { get; private set; }

        public override void Contextualize([NotNull] ViewContext viewContext)
        {
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
        public HtmlString CheckBoxFor(
            [NotNull] Expression<Func<TModel, bool>> expression,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateCheckBox(modelExplorer, GetExpressionName(expression), isChecked: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString DropDownListFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            string optionLabel,
            object htmlAttributes)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);

            return GenerateDropDown(modelExplorer, ExpressionHelper.GetExpressionText(expression), selectList,
                optionLabel, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString DisplayFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression,
                                                                           ViewData,
                                                                           MetadataProvider);

            return GenerateDisplay(modelExplorer,
                                   htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                                   templateName,
                                   additionalViewData);
        }

        /// <inheritdoc />
        public string DisplayNameFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateDisplayName(modelExplorer, ExpressionHelper.GetExpressionText(expression));
        }

        /// <inheritdoc />
        public string DisplayNameForInnerType<TModelItem, TResult>(
            [NotNull] Expression<Func<TModelItem, TResult>> expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression<TModelItem, TResult>(
                expression,
                new ViewDataDictionary<TModelItem>(ViewData, model: null),
                MetadataProvider);

            var expressionText = ExpressionHelper.GetExpressionText(expression);
            if (modelExplorer == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionText));
            }

            return GenerateDisplayName(modelExplorer, expressionText);
        }

        /// <inheritdoc />
        public string DisplayTextFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return GenerateDisplayText(GetModelExplorer(expression));
        }

        /// <inheritdoc />
        public HtmlString EditorFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);

            return GenerateEditor(
                modelExplorer,
                htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                templateName,
                additionalViewData);
        }

        /// <inheritdoc />
        public HtmlString HiddenFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateHidden(
                modelExplorer,
                GetExpressionName(expression),
                modelExplorer.Model,
                useViewData: false,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string IdFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return GenerateId(GetExpressionName(expression));
        }

        /// <inheritdoc />
        public HtmlString LabelFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string labelText,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateLabel(
                modelExplorer,
                ExpressionHelper.GetExpressionText(expression),
                labelText,
                htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString ListBoxFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            var name = ExpressionHelper.GetExpressionText(expression);

            return GenerateListBox(modelExplorer, name, selectList, htmlAttributes);
        }

        /// <inheritdoc />
        public string NameFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            var expressionName = GetExpressionName(expression);
            return Name(expressionName);
        }

        /// <inheritdoc />
        public HtmlString PasswordFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GeneratePassword(
                modelExplorer,
                GetExpressionName(expression),
                value: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString RadioButtonFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            [NotNull] object value,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateRadioButton(
                modelExplorer,
                GetExpressionName(expression),
                value,
                isChecked: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString TextAreaFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            int rows,
            int columns,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateTextArea(modelExplorer, GetExpressionName(expression), rows, columns, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString TextBoxFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string format,
            object htmlAttributes)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateTextBox(
                modelExplorer,
                GetExpressionName(expression),
                modelExplorer.Model,
                format,
                htmlAttributes);
        }

        protected string GetExpressionName<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            return ExpressionHelper.GetExpressionText(expression);
        }

        protected ModelExplorer GetModelExplorer<TResult>([NotNull] Expression<Func<TModel, TResult>> expression)
        {
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);
            if (modelExplorer == null)
            {
                var expressionName = GetExpressionName(expression);
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionName));
            }

            return modelExplorer;
        }

        /// <inheritdoc />
        public HtmlString ValidationMessageFor<TResult>(
            [NotNull] Expression<Func<TModel, TResult>> expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            return GenerateValidationMessage(ExpressionHelper.GetExpressionText(expression),
                message,
                htmlAttributes,
                tag);
        }

        /// <inheritdoc />
        public string ValueFor<TResult>([NotNull] Expression<Func<TModel, TResult>> expression, string format)
        {
            var modelExplorer = GetModelExplorer(expression);
            return GenerateValue(
                ExpressionHelper.GetExpressionText(expression),
                modelExplorer.Model,
                format,
                useViewData: false);
        }
    }
}

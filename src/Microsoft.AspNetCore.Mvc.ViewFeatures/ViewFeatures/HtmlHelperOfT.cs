// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class HtmlHelper<TModel> : HtmlHelper, IHtmlHelper<TModel>
    {
        private readonly ExpressionTextCache _expressionTextCache;

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
            ExpressionTextCache expressionTextCache)
            : base(
                  htmlGenerator,
                  viewEngine,
                  metadataProvider,
                  bufferScope,
                  htmlEncoder,
                  urlEncoder)
        {
            if (expressionTextCache == null)
            {
                throw new ArgumentNullException(nameof(expressionTextCache));
            }

            _expressionTextCache = expressionTextCache;
        }

        /// <inheritdoc />
        public new ViewDataDictionary<TModel> ViewData { get; private set; }

        public override void Contextualize(ViewContext viewContext)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

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
        public IHtmlContent CheckBoxFor(
            Expression<Func<TModel, bool>> expression,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateCheckBox(
                modelExplorer,
                GetExpressionName(expression),
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
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateDropDown(
                modelExplorer,
                GetExpressionName(expression),
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
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateDisplay(
                modelExplorer,
                htmlFieldName ?? GetExpressionName(expression),
                templateName,
                additionalViewData);
        }

        /// <inheritdoc />
        public string DisplayNameFor<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateDisplayName(modelExplorer, GetExpressionName(expression));
        }

        /// <inheritdoc />
        public string DisplayNameForInnerType<TModelItem, TResult>(
            Expression<Func<TModelItem, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression<TModelItem, TResult>(
                expression,
                new ViewDataDictionary<TModelItem>(ViewData, model: null),
                MetadataProvider);

            var expressionText = ExpressionHelper.GetExpressionText(expression, _expressionTextCache);
            if (modelExplorer == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionText));
            }

            return GenerateDisplayName(modelExplorer, expressionText);
        }

        /// <inheritdoc />
        public string DisplayTextFor<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GenerateDisplayText(GetModelExplorer(expression));
        }

        /// <inheritdoc />
        public IHtmlContent EditorFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateEditor(
                modelExplorer,
                htmlFieldName ?? GetExpressionName(expression),
                templateName,
                additionalViewData);
        }

        /// <inheritdoc />
        public IHtmlContent HiddenFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateHidden(
                modelExplorer,
                GetExpressionName(expression),
                modelExplorer.Model,
                useViewData: false,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string IdFor<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return GenerateId(GetExpressionName(expression));
        }

        /// <inheritdoc />
        public IHtmlContent LabelFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            string labelText,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateLabel(modelExplorer, GetExpressionName(expression), labelText, htmlAttributes);
        }

        /// <inheritdoc />
        public IHtmlContent ListBoxFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            var name = GetExpressionName(expression);

            return GenerateListBox(modelExplorer, name, selectList, htmlAttributes);
        }

        /// <inheritdoc />
        public string NameFor<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var expressionName = GetExpressionName(expression);
            return Name(expressionName);
        }

        /// <inheritdoc />
        public IHtmlContent PasswordFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GeneratePassword(
                modelExplorer,
                GetExpressionName(expression),
                value: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public IHtmlContent RadioButtonFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            object value,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateRadioButton(
                modelExplorer,
                GetExpressionName(expression),
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
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateTextArea(modelExplorer, GetExpressionName(expression), rows, columns, htmlAttributes);
        }

        /// <inheritdoc />
        public IHtmlContent TextBoxFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            string format,
            object htmlAttributes)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateTextBox(
                modelExplorer,
                GetExpressionName(expression),
                modelExplorer.Model,
                format,
                htmlAttributes);
        }

        protected string GetExpressionName<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return ExpressionHelper.GetExpressionText(expression, _expressionTextCache);
        }

        protected ModelExplorer GetModelExplorer<TResult>(Expression<Func<TModel, TResult>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer =
                ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);
            if (modelExplorer == null)
            {
                var expressionName = GetExpressionName(expression);
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionName));
            }

            return modelExplorer;
        }

        /// <inheritdoc />
        public IHtmlContent ValidationMessageFor<TResult>(
            Expression<Func<TModel, TResult>> expression,
            string message,
            object htmlAttributes,
            string tag)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateValidationMessage(
                modelExplorer,
                GetExpressionName(expression),
                message,
                tag,
                htmlAttributes);
        }

        /// <inheritdoc />
        public string ValueFor<TResult>(Expression<Func<TModel, TResult>> expression, string format)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var modelExplorer = GetModelExplorer(expression);
            return GenerateValue(GetExpressionName(expression), modelExplorer.Model, format, useViewData: false);
        }
    }
}

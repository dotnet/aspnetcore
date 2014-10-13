// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;

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
            [NotNull] IModelMetadataProvider metadataProvider)
            : base(htmlGenerator, viewEngine, metadataProvider)
        {
        }

        /// <inheritdoc />
        public new ViewDataDictionary<TModel> ViewData { get; private set; }

        public override void Contextualize([NotNull] ViewContext viewContext)
        {
            if (viewContext.ViewData == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                        "ViewData",
                        typeof(ViewContext)),
                    "viewContext");
            }

            ViewData = viewContext.ViewData as ViewDataDictionary<TModel>;
            if (ViewData == null)
            {
                // viewContext may contain a base ViewDataDictionary instance. So complain about that type, not TModel.
                throw new ArgumentException(Resources.FormatArgumentPropertyUnexpectedType(
                        "ViewData",
                        viewContext.ViewData.GetType().FullName,
                        typeof(ViewDataDictionary<TModel>).FullName),
                    "viewContext");
            }

            base.Contextualize(viewContext);
        }

        /// <inheritdoc />
        public HtmlString CheckBoxFor([NotNull] Expression<Func<TModel, bool>> expression,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateCheckBox(metadata, GetExpressionName(expression), isChecked: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString DropDownListFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList, string optionLabel, object htmlAttributes)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);

            return GenerateDropDown(metadata, ExpressionHelper.GetExpressionText(expression), selectList,
                optionLabel, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString DisplayFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression,
                                             string templateName,
                                             string htmlFieldName,
                                             object additionalViewData)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression,
                                                                           ViewData,
                                                                           MetadataProvider);

            return GenerateDisplay(metadata,
                                   htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                                   templateName,
                                   additionalViewData);
        }

        /// <inheritdoc />
        public string DisplayNameFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateDisplayName(metadata, ExpressionHelper.GetExpressionText(expression));
        }

        /// <inheritdoc />
        public string DisplayNameForInnerType<TModelItem, TValue>(
            [NotNull] Expression<Func<TModelItem, TValue>> expression)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression<TModelItem, TValue>(
                expression,
                new ViewDataDictionary<TModelItem>(MetadataProvider),
                MetadataProvider);

            var expressionText = ExpressionHelper.GetExpressionText(expression);
            if (metadata == null)
            {
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionText));
            }

            return GenerateDisplayName(metadata, expressionText);
        }

        /// <inheritdoc />
        public string DisplayTextFor<TValue>([NotNull] Expression<Func<TModel, TValue>> expression)
        {
            return GenerateDisplayText(GetModelMetadata(expression));
        }

        /// <inheritdoc />
        public HtmlString EditorFor<TValue>(
            [NotNull] Expression<Func<TModel, TValue>> expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);

            return GenerateEditor(
                metadata,
                htmlFieldName ?? ExpressionHelper.GetExpressionText(expression),
                templateName,
                additionalViewData);
        }

        /// <inheritdoc />
        public HtmlString HiddenFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateHidden(metadata, GetExpressionName(expression), metadata.Model, useViewData: false,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public string IdFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return GenerateId(GetExpressionName(expression));
        }

        /// <inheritdoc />
        public HtmlString LabelFor<TValue>(
            [NotNull] Expression<Func<TModel, TValue>> expression,
            string labelText,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateLabel(metadata, ExpressionHelper.GetExpressionText(expression), labelText, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString ListBoxFor<TProperty>(
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            IEnumerable<SelectListItem> selectList,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            var name = ExpressionHelper.GetExpressionText(expression);

            return GenerateListBox(metadata, name, selectList, htmlAttributes);
        }

        /// <inheritdoc />
        public string NameFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            var expressionName = GetExpressionName(expression);
            return Name(expressionName);
        }

        /// <inheritdoc />
        public HtmlString PasswordFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GeneratePassword(metadata, GetExpressionName(expression), value: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString RadioButtonFor<TProperty>(
            [NotNull] Expression<Func<TModel, TProperty>> expression,
            [NotNull] object value,
            object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateRadioButton(metadata, GetExpressionName(expression), value, isChecked: null,
                htmlAttributes: htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString TextAreaFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, int rows,
            int columns, object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateTextArea(metadata, GetExpressionName(expression), rows, columns, htmlAttributes);
        }

        /// <inheritdoc />
        public HtmlString TextBoxFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            string format, object htmlAttributes)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateTextBox(metadata, GetExpressionName(expression), metadata.Model, format, htmlAttributes);
        }

        protected string GetExpressionName<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            return ExpressionHelper.GetExpressionText(expression);
        }

        protected ModelMetadata GetModelMetadata<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, MetadataProvider);
            if (metadata == null)
            {
                var expressionName = GetExpressionName(expression);
                throw new InvalidOperationException(Resources.FormatHtmlHelper_NullModelMetadata(expressionName));
            }

            return metadata;
        }

        /// <inheritdoc />
        public HtmlString ValidationMessageFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
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
        public string ValueFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression, string format)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateValue(ExpressionHelper.GetExpressionText(expression), metadata.Model, format,
                useViewData: false);
        }
    }
}

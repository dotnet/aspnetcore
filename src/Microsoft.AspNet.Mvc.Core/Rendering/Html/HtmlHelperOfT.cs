using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
            [NotNull] IViewEngine viewEngine, 
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] IUrlHelper urlHelper)
             : base(viewEngine, metadataProvider, urlHelper)
        {
        }

        /// <inheritdoc />
        public new ViewDataDictionary<TModel> ViewData { get; private set;}

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
        public HtmlString NameFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression)
        {
            var expressionName = GetExpressionName(expression);
            return Name(expressionName);
        }

        /// <inheritdoc />
        public HtmlString TextBoxFor<TProperty>([NotNull] Expression<Func<TModel, TProperty>> expression,
            string format, IDictionary<string, object> htmlAttributes)
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

        public HtmlString ValueFor<TProperty>(Expression<Func<TModel, TProperty>> expression, string format)
        {
            var metadata = GetModelMetadata(expression);
            return GenerateValue(ExpressionHelper.GetExpressionText(expression), metadata.Model, format, useViewData: false);
        }
    }
}

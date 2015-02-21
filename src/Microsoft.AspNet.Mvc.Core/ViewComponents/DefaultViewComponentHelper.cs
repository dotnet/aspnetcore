// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultViewComponentHelper : IViewComponentHelper, ICanHasViewContext
    {
        private readonly IViewComponentInvokerFactory _invokerFactory;
        private readonly IViewComponentSelector _selector;
        private ViewContext _viewContext;

        public DefaultViewComponentHelper(
            [NotNull] IViewComponentSelector selector,
            [NotNull] IViewComponentInvokerFactory invokerFactory)
        {
            _selector = selector;
            _invokerFactory = invokerFactory;
        }

        public void Contextualize([NotNull] ViewContext viewContext)
        {
            _viewContext = viewContext;
        }

        public HtmlString Invoke([NotNull] string name, params object[] args)
        {
            var componentType = SelectComponent(name);
            return Invoke(componentType, args);
        }

        public HtmlString Invoke([NotNull] Type componentType, params object[] args)
        {
            using (var writer = new StringWriter())
            {
                InvokeCore(writer, componentType, args);
                return new HtmlString(writer.ToString());
            }
        }

        public void RenderInvoke([NotNull] string name, params object[] args)
        {
            var componentType = SelectComponent(name);
            InvokeCore(_viewContext.Writer, componentType, args);
        }

        public void RenderInvoke([NotNull] Type componentType, params object[] args)
        {
            InvokeCore(_viewContext.Writer, componentType, args);
        }

        public async Task<HtmlString> InvokeAsync([NotNull] string name, params object[] args)
        {
            var componentType = SelectComponent(name);
            return await InvokeAsync(componentType, args);
        }

        public async Task<HtmlString> InvokeAsync([NotNull] Type componentType, params object[] args)
        {
            using (var writer = new StringWriter())
            {
                await InvokeCoreAsync(writer, componentType, args);
                return new HtmlString(writer.ToString());
            }
        }

        public async Task RenderInvokeAsync([NotNull] string name, params object[] args)
        {
            var componentType = SelectComponent(name);
            await InvokeCoreAsync(_viewContext.Writer, componentType, args);
        }

        public async Task RenderInvokeAsync([NotNull] Type componentType, params object[] args)
        {
            await InvokeCoreAsync(_viewContext.Writer, componentType, args);
        }

        private Type SelectComponent([NotNull] string name)
        {
            var componentType = _selector.SelectComponent(name);
            if (componentType == null)
            {
                throw new InvalidOperationException(Resources.FormatViewComponent_CannotFindComponent(name));
            }

            return componentType;
        }

        private async Task InvokeCoreAsync([NotNull] TextWriter writer, [NotNull] Type componentType, object[] args)
        {
            var invoker = _invokerFactory.CreateInstance(componentType.GetTypeInfo(), args);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(componentType));
            }

            var context = new ViewComponentContext(componentType.GetTypeInfo(), _viewContext, writer);
            await invoker.InvokeAsync(context);
        }

        private void InvokeCore([NotNull] TextWriter writer, [NotNull] Type componentType, object[] arguments)
        {
            var invoker = _invokerFactory.CreateInstance(componentType.GetTypeInfo(), arguments);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(componentType));
            }

            var context = new ViewComponentContext(componentType.GetTypeInfo(), _viewContext, writer);
            invoker.Invoke(context);
        }
    }
}

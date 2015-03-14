// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentHelper : IViewComponentHelper, ICanHasViewContext
    {
        private readonly IViewComponentDescriptorCollectionProvider _descriptorProvider;
        private readonly IViewComponentInvokerFactory _invokerFactory;
        private readonly IViewComponentSelector _selector;
        private ViewContext _viewContext;

        public DefaultViewComponentHelper(
            [NotNull] IViewComponentDescriptorCollectionProvider descriptorProvider,
            [NotNull] IViewComponentSelector selector,
            [NotNull] IViewComponentInvokerFactory invokerFactory)
        {
            _descriptorProvider = descriptorProvider;
            _selector = selector;
            _invokerFactory = invokerFactory;
        }

        public void Contextualize([NotNull] ViewContext viewContext)
        {
            _viewContext = viewContext;
        }

        public HtmlString Invoke([NotNull] string name, params object[] arguments)
        {
            var descriptor = SelectComponent(name);

            using (var writer = new StringWriter())
            {
                InvokeCore(writer, descriptor, arguments);
                return new HtmlString(writer.ToString());
            }
        }

        public HtmlString Invoke([NotNull] Type componentType, params object[] arguments)
        {
            var descriptor = SelectComponent(componentType);

            using (var writer = new StringWriter())
            {
                InvokeCore(writer, descriptor, arguments);
                return new HtmlString(writer.ToString());
            }
        }

        public void RenderInvoke([NotNull] string name, params object[] arguments)
        {
            var descriptor = SelectComponent(name);
            InvokeCore(_viewContext.Writer, descriptor, arguments);
        }

        public void RenderInvoke([NotNull] Type componentType, params object[] arguments)
        {
            var descriptor = SelectComponent(componentType);
            InvokeCore(_viewContext.Writer, descriptor, arguments);
        }

        public async Task<HtmlString> InvokeAsync([NotNull] string name, params object[] arguments)
        {
            var descriptor = SelectComponent(name);

            using (var writer = new StringWriter())
            {
                await InvokeCoreAsync(writer, descriptor, arguments);
                return new HtmlString(writer.ToString());
            }
        }

        public async Task<HtmlString> InvokeAsync([NotNull] Type componentType, params object[] arguments)
        {
            var descriptor = SelectComponent(componentType);

            using (var writer = new StringWriter())
            {
                await InvokeCoreAsync(writer, descriptor, arguments);
                return new HtmlString(writer.ToString());
            }
        }

        public async Task RenderInvokeAsync([NotNull] string name, params object[] arguments)
        {
            var descriptor = SelectComponent(name);
            await InvokeCoreAsync(_viewContext.Writer, descriptor, arguments);
        }

        public async Task RenderInvokeAsync([NotNull] Type componentType, params object[] arguments)
        {
            var descriptor = SelectComponent(componentType);
            await InvokeCoreAsync(_viewContext.Writer, descriptor, arguments);
        }

        private ViewComponentDescriptor SelectComponent(string name)
        {
            var descriptor = _selector.SelectComponent(name);
            if (descriptor == null)
            {
                throw new InvalidOperationException(Resources.FormatViewComponent_CannotFindComponent(name));
            }

            return descriptor;
        }

        private ViewComponentDescriptor SelectComponent(Type componentType)
        {
            var descriptors = _descriptorProvider.ViewComponents;
            foreach (var descriptor in descriptors.Items)
            {
                if (descriptor.Type == componentType)
                {
                    return descriptor;
                }
            }

            throw new InvalidOperationException(Resources.FormatViewComponent_CannotFindComponent(
                componentType.FullName));
        }

        private async Task InvokeCoreAsync(
            [NotNull] TextWriter writer,
            [NotNull] ViewComponentDescriptor descriptor,
            object[] arguments)
        {
            var invoker = _invokerFactory.CreateInstance(descriptor, arguments);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(descriptor.Type.FullName));
            }

            var context = new ViewComponentContext(descriptor, arguments, _viewContext, writer);
            await invoker.InvokeAsync(context);
        }

        private void InvokeCore(
            [NotNull] TextWriter writer,
            [NotNull] ViewComponentDescriptor descriptor,
            object[] arguments)
        {
            var invoker = _invokerFactory.CreateInstance(descriptor, arguments);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(descriptor.Type.FullName));
            }

            var context = new ViewComponentContext(descriptor, arguments, _viewContext, writer);
            invoker.Invoke(context);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Mvc.ViewFeatures.Internal;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class DefaultViewComponentHelper : IViewComponentHelper, ICanHasViewContext
    {
        private readonly IViewComponentDescriptorCollectionProvider _descriptorProvider;
        private readonly IViewComponentInvokerFactory _invokerFactory;
        private readonly IViewComponentSelector _selector;
        private ViewContext _viewContext;

        public DefaultViewComponentHelper(
            IViewComponentDescriptorCollectionProvider descriptorProvider,
            IViewComponentSelector selector,
            IViewComponentInvokerFactory invokerFactory)
        {
            if (descriptorProvider == null)
            {
                throw new ArgumentNullException(nameof(descriptorProvider));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (invokerFactory == null)
            {
                throw new ArgumentNullException(nameof(invokerFactory));
            }

            _descriptorProvider = descriptorProvider;
            _selector = selector;
            _invokerFactory = invokerFactory;
        }

        public void Contextualize(ViewContext viewContext)
        {
            if (viewContext == null)
            {
                throw new ArgumentNullException(nameof(viewContext));
            }

            _viewContext = viewContext;
        }

        public IHtmlContent Invoke(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var descriptor = SelectComponent(name);

            using (var writer = new StringCollectionTextWriter(_viewContext.Writer.Encoding))
            {
                InvokeCore(writer, descriptor, arguments);
                return writer.Content;
            }
        }

        public IHtmlContent Invoke(Type componentType, params object[] arguments)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var descriptor = SelectComponent(componentType);

            using (var writer = new StringCollectionTextWriter(_viewContext.Writer.Encoding))
            {
                InvokeCore(writer, descriptor, arguments);
                return writer.Content;
            }
        }

        public void RenderInvoke(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var descriptor = SelectComponent(name);
            InvokeCore(_viewContext.Writer, descriptor, arguments);
        }

        public void RenderInvoke(Type componentType, params object[] arguments)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var descriptor = SelectComponent(componentType);
            InvokeCore(_viewContext.Writer, descriptor, arguments);
        }

        public async Task<IHtmlContent> InvokeAsync(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var descriptor = SelectComponent(name);

            using (var writer = new StringCollectionTextWriter(_viewContext.Writer.Encoding))
            {
                await InvokeCoreAsync(writer, descriptor, arguments);
                return writer.Content;
            }
        }

        public async Task<IHtmlContent> InvokeAsync(Type componentType, params object[] arguments)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var descriptor = SelectComponent(componentType);

            using (var writer = new StringCollectionTextWriter(_viewContext.Writer.Encoding))
            {
                await InvokeCoreAsync(writer, descriptor, arguments);
                return writer.Content;
            }
        }

        public Task RenderInvokeAsync(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var descriptor = SelectComponent(name);
            return InvokeCoreAsync(_viewContext.Writer, descriptor, arguments);
        }

        public Task RenderInvokeAsync(Type componentType, params object[] arguments)
        {
            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            var descriptor = SelectComponent(componentType);
            return InvokeCoreAsync(_viewContext.Writer, descriptor, arguments);
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

        private Task InvokeCoreAsync(
            TextWriter writer,
            ViewComponentDescriptor descriptor,
            object[] arguments)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var context = new ViewComponentContext(descriptor, arguments, _viewContext, writer);

            var invoker = _invokerFactory.CreateInstance(context);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(descriptor.Type.FullName));
            }

            return invoker.InvokeAsync(context);
        }

        private void InvokeCore(
            TextWriter writer,
            ViewComponentDescriptor descriptor,
            object[] arguments)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            var context = new ViewComponentContext(descriptor, arguments, _viewContext, writer);

            var invoker = _invokerFactory.CreateInstance(context);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(descriptor.Type.FullName));
            }

            invoker.Invoke(context);
        }
    }
}

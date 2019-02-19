// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Provides methods for building a collection of <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    public class RenderTreeBuilder
    {
        public void OpenElement(int sequence, string elementName)
        {
        }

        public void CloseElement()
        {
        }

        public void AddMarkupContent(int sequence, string markupContent)
        {
        }

        public void AddContent(int sequence, string textContent)
        {
        }

        public void AddContent(int sequence, RenderFragment fragment)
        {
        }

        public void AddContent<T>(int sequence, RenderFragment<T> fragment, T value)
        {
        }

        public void AddContent(int sequence, MarkupString markupContent)
        {
        }

        public void AddContent(int sequence, object textContent)
        {
        }

        public void AddAttribute(int sequence, string name, bool value)
        {
        }

        public void AddAttribute(int sequence, string name, string value)
        {
        }

        public void AddAttribute(int sequence, string name, Action value)
        {
        }

        public void AddAttribute(int sequence, string name, Action<UIEventArgs> value)
        {
        }

        public void AddAttribute(int sequence, string name, Func<Task> value)
        {
        }

        public void AddAttribute(int sequence, string name, Func<UIEventArgs, Task> value)
        {
        }

        public void AddAttribute(int sequence, string name, MulticastDelegate value)
        {
        }

        public void AddAttribute(int sequence, string name, EventCallback value)
        {
        }

        public void AddAttribute<T>(int sequence, string name, EventCallback<T> value)
        {
        }

        public void AddAttribute(int sequence, string name, object value)
        {
        }

        public void OpenComponent<TComponent>(int sequence) where TComponent : IComponent
        {
        }

        public void OpenComponent(int sequence, Type componentType)
        {
        }

        public void CloseComponent()
        {
        }
        
        public void AddElementReferenceCapture(int sequence, Action<ElementRef> elementReferenceCaptureAction)
        {
        }

        public void AddComponentReferenceCapture(int sequence, Action<object> componentReferenceCaptureAction)
        {
        }
    }
}
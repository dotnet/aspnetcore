// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR.Client;

namespace Ignitor
{
    public class ElementNode : ContainerNode
    {
        private readonly Dictionary<string, object> _attributes;
        private readonly Dictionary<string, object> _properties;
        private readonly Dictionary<string, ElementEventDescriptor> _events;

        public ElementNode(string tagName)
        {
            TagName = tagName ?? throw new ArgumentNullException(nameof(tagName));
            _attributes = new Dictionary<string, object>(StringComparer.Ordinal);
            _properties = new Dictionary<string, object>(StringComparer.Ordinal);
            _events = new Dictionary<string, ElementEventDescriptor>(StringComparer.Ordinal);
        }
        public string TagName { get; }

        public IReadOnlyDictionary<string, object> Attributes => _attributes;

        public IReadOnlyDictionary<string, object> Properties => _properties;

        public IReadOnlyDictionary<string, ElementEventDescriptor> Events => _events;

        public void SetAttribute(string key, object value)
        {
            _attributes[key] = value;
        }

        public void RemoveAttribute(string key)
        {
            _attributes.Remove(key);
        }

        public void SetProperty(string key, object value)
        {
            _properties[key] = value;
        }

        public void SetEvent(string eventName, ElementEventDescriptor descriptor)
        {
            if (eventName is null)
            {
                throw new ArgumentNullException(nameof(eventName));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            _events[eventName] = descriptor;
        }

        internal async Task SelectAsync(HubConnection connection, string value)
        {
            if (!Events.TryGetValue("change", out var changeEventDescriptor))
            {
                throw new InvalidOperationException("Element does not have a click event.");
            }

            var mouseEventArgs = new UIChangeEventArgs()
            {
                Type = changeEventDescriptor.EventName,
                Value = value
            };

            var browserDescriptor = new RendererRegistryEventDispatcher.BrowserEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = changeEventDescriptor.EventId,
                EventArgsType = "change",
                EventFieldInfo = new EventFieldInfo
                {
                    ComponentId = 0,
                    FieldValue = value
                }
            };

            var serializedJson = JsonSerializer.Serialize(mouseEventArgs, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var argsObject = new object[] { browserDescriptor, serializedJson };
            var callId = "0";
            var assemblyName = "Microsoft.AspNetCore.Components.Web";
            var methodIdentifier = "DispatchEvent";
            var dotNetObjectId = 0;
            var clickArgs = JsonSerializer.Serialize(argsObject, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await connection.InvokeAsync("BeginInvokeDotNetFromJS", callId, assemblyName, methodIdentifier, dotNetObjectId, clickArgs);

        }

        public class ElementEventDescriptor
        {
            public ElementEventDescriptor(string eventName, int eventId)
            {
                EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
                EventId = eventId;
            }

            public string EventName { get; }

            public int EventId { get; }
        }

        public async Task ClickAsync(HubConnection connection)
        {
            if (!Events.TryGetValue("click", out var clickEventDescriptor))
            {
                throw new InvalidOperationException("Element does not have a click event.");
            }

            var mouseEventArgs = new UIMouseEventArgs()
            {
                Type = clickEventDescriptor.EventName,
                Detail = 1
            };
            var browserDescriptor = new RendererRegistryEventDispatcher.BrowserEventDescriptor()
            {
                BrowserRendererId = 0,
                EventHandlerId = clickEventDescriptor.EventId,
                EventArgsType = "mouse",
            };
            var serializedJson = JsonSerializer.Serialize(mouseEventArgs, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var argsObject = new object[] { browserDescriptor, serializedJson };
            var callId = "0";
            var assemblyName = "Microsoft.AspNetCore.Components.Web";
            var methodIdentifier = "DispatchEvent";
            var dotNetObjectId = 0;
            var clickArgs = JsonSerializer.Serialize(argsObject, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await connection.InvokeAsync("BeginInvokeDotNetFromJS", callId, assemblyName, methodIdentifier, dotNetObjectId, clickArgs);
        }
    }
}

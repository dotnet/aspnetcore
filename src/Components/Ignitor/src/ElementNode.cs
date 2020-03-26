// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

#nullable enable
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

        internal Task SelectAsync(HubConnection connection, string value)
        {
            if (!Events.TryGetValue("change", out var changeEventDescriptor))
            {
                throw new InvalidOperationException("Element does not have a change event.");
            }

            var args = new
            {
                Value = value
            };

            var webEventDescriptor = new
            {
                BrowserRendererId = 0,
                EventHandlerId = changeEventDescriptor.EventId,
                EventArgsType = "change",
                EventFieldInfo = new
                {
                    ComponentId = 0,
                    FieldValue = value
                }
            };

            return DispatchEventCore(connection, Serialize(webEventDescriptor), Serialize(args));
        }

        internal Task ClickAsync(HubConnection connection)
        {
            if (!Events.TryGetValue("click", out var clickEventDescriptor))
            {
                throw new InvalidOperationException("Element does not have a click event.");
            }

            var mouseEventArgs = new
            {
                Type = clickEventDescriptor.EventName,
                Detail = 1
            };
            var webEventDescriptor = new
            {
                BrowserRendererId = 0,
                EventHandlerId = clickEventDescriptor.EventId,
                EventArgsType = "mouse",
            };

            return DispatchEventCore(connection, Serialize(webEventDescriptor), Serialize(mouseEventArgs));
        }

        private static string Serialize<T>(T payload) =>
             JsonSerializer.Serialize(payload, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        private static Task DispatchEventCore(HubConnection connection, string descriptor, string eventArgs) =>
            connection.InvokeAsync("DispatchBrowserEvent", descriptor, eventArgs);

        public class ElementEventDescriptor
        {
            public ElementEventDescriptor(string eventName, ulong eventId)
            {
                EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
                EventId = eventId;
            }

            public string EventName { get; }

            public ulong EventId { get; }
        }
    }
}
#nullable restore

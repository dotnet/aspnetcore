using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.WebView.Document
{
    internal class ElementNode : ContainerNode
    {
        private readonly Dictionary<string, object> _attributes;
        private readonly Dictionary<string, object> _properties;
        private readonly Dictionary<string, ElementEventDescriptor> _events;

        public ElementNode(string elementName)
        {
            TagName = elementName;
            _attributes = new Dictionary<string, object>(StringComparer.Ordinal);
            _properties = new Dictionary<string, object>(StringComparer.Ordinal);
            _events = new Dictionary<string, ElementEventDescriptor>(StringComparer.Ordinal);
        }

        public string TagName { get; }

        public IReadOnlyDictionary<string, object> Attributes => _attributes;

        public IReadOnlyDictionary<string, object> Properties => _properties;

        public IReadOnlyDictionary<string, ElementEventDescriptor> Events => _events;

        internal void RemoveAttribute(string key)
        {
            _attributes.Remove(key);
        }

        internal void SetAttribute(string key, object value)
        {
            _attributes[key] = value;
        }

        internal void SetEvent(string eventName, ElementEventDescriptor descriptor)
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

        internal void SetProperty(string key, object value)
        {
            _properties[key] = value;
        }

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

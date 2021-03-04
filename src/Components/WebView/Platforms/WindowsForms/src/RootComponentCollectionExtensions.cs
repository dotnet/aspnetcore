using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.Components.WebView.WindowsForms
{
    public static class RootComponentCollectionExtensions
    {
        public static void Add<TComponent>(this ObservableCollection<RootComponent> components, string selector, IDictionary<string, object> parameters = null)
            where TComponent : IComponent
        {
            components.Add(new RootComponent(selector, typeof(TComponent), parameters));
        }

        public static void Remove(this ObservableCollection<RootComponent> components, string selector)
        {
            for (var i = 0; i < components.Count; i++)
            {
                if (components[i].Selector.Equals(selector, StringComparison.Ordinal))
                {
                    components.RemoveAt(i);
                    return;
                }
            }

            throw new ArgumentException($"There is no root component with selector '{selector}'");
        }
    }
}

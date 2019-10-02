// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A component that provides a cascading value to all descendant components.
    /// </summary>
    public class CascadingValue<TValue> : ICascadingValueComponent, IComponent
    {
        private RenderHandle _renderHandle;
        private HashSet<ComponentState> _subscribers; // Lazily instantiated
        private bool _hasSetParametersPreviously;

        /// <summary>
        /// The content to which the value should be provided.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// The value to be provided.
        /// </summary>
        [Parameter] public TValue Value { get; set; }

        /// <summary>
        /// Optionally gives a name to the provided value. Descendant components
        /// will be able to receive the value by specifying this name.
        ///
        /// If no name is specified, then descendant components will receive the
        /// value based the type of value they are requesting.
        /// </summary>
        [Parameter] public string Name { get; set; }

        /// <summary>
        /// If true, indicates that <see cref="Value"/> will not change. This is a
        /// performance optimization that allows the framework to skip setting up
        /// change notifications. Set this flag only if you will not change
        /// <see cref="Value"/> during the component's lifetime.
        /// </summary>
        [Parameter] public bool IsFixed { get; set; }

        object ICascadingValueComponent.CurrentValue => Value;

        bool ICascadingValueComponent.CurrentValueIsFixed => IsFixed;

        /// <inheritdoc />
        public void Attach(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        /// <inheritdoc />
        public Task SetParametersAsync(ParameterView parameters)
        {
            // Implementing the parameter binding manually, instead of just calling
            // parameters.SetParameterProperties(this), is just a very slight perf optimization
            // and makes it simpler impose rules about the params being required or not.

            var hasSuppliedValue = false;
            var previousValue = Value;
            var previousFixed = IsFixed;
            Value = default;
            ChildContent = null;
            Name = null;
            IsFixed = false;

            foreach (var parameter in parameters)
            {
                if (parameter.Name.Equals(nameof(Value), StringComparison.OrdinalIgnoreCase))
                {
                    Value = (TValue)parameter.Value;
                    hasSuppliedValue = true;
                }
                else if (parameter.Name.Equals(nameof(ChildContent), StringComparison.OrdinalIgnoreCase))
                {
                    ChildContent = (RenderFragment)parameter.Value;
                }
                else if (parameter.Name.Equals(nameof(Name), StringComparison.OrdinalIgnoreCase))
                {
                    Name = (string)parameter.Value;
                    if (string.IsNullOrEmpty(Name))
                    {
                        throw new ArgumentException($"The parameter '{nameof(Name)}' for component '{nameof(CascadingValue<TValue>)}' does not allow null or empty values.");
                    }
                }
                else if (parameter.Name.Equals(nameof(IsFixed), StringComparison.OrdinalIgnoreCase))
                {
                    IsFixed = (bool)parameter.Value;
                }
                else
                {
                    throw new ArgumentException($"The component '{nameof(CascadingValue<TValue>)}' does not accept a parameter with the name '{parameter.Name}'.");
                }
            }

            if (_hasSetParametersPreviously && IsFixed != previousFixed)
            {
                throw new InvalidOperationException($"The value of {nameof(IsFixed)} cannot be changed dynamically.");
            }

            _hasSetParametersPreviously = true;

            // It's OK for the value to be null, but some "Value" param must be supplied
            // because it serves no useful purpose to have a <CascadingValue> otherwise.
            if (!hasSuppliedValue)
            {
                throw new ArgumentException($"Missing required parameter '{nameof(Value)}' for component '{GetType().Name}'.");
            }

            // Rendering is most efficient when things are queued from rootmost to leafmost.
            // Given a components A (parent) -> B (child), you want them to be queued in order
            // [A, B] because if you had [B, A], then the render for A might change B's params
            // making it render again, so you'd render [B, A, B], which is wasteful.
            // At some point we might consider making the render queue actually enforce this
            // ordering during insertion.
            //
            // For the CascadingValue component, this observation is why it's important to render
            // ourself before notifying subscribers (which can be grandchildren or deeper).
            // If we rerendered subscribers first, then our own subsequent render might cause an
            // further update that makes those nested subscribers get rendered twice.
            _renderHandle.Render(Render);

            if (_subscribers != null && ChangeDetection.MayHaveChanged(previousValue, Value))
            {
                NotifySubscribers(parameters.Lifetime);
            }

            return Task.CompletedTask;
        }

        bool ICascadingValueComponent.CanSupplyValue(Type requestedType, string requestedName)
        {
            if (!requestedType.IsAssignableFrom(typeof(TValue)))
            {
                return false;
            }

            return (requestedName == null && Name == null) // Match on type alone
                || string.Equals(requestedName, Name, StringComparison.OrdinalIgnoreCase); // Also match on name
        }

        void ICascadingValueComponent.Subscribe(ComponentState subscriber)
        {
#if DEBUG
            if (IsFixed)
            {
                // Should not be possible. User code cannot trigger this.
                // Checking only to catch possible future framework bugs.
                throw new InvalidOperationException($"Cannot subscribe to a {typeof(CascadingValue<>).Name} when {nameof(IsFixed)} is true.");
            }
#endif

            if (_subscribers == null)
            {
                 _subscribers = new HashSet<ComponentState>();
            }

            _subscribers.Add(subscriber);
        }

        void ICascadingValueComponent.Unsubscribe(ComponentState subscriber)
        {
            _subscribers.Remove(subscriber);
        }

        private void NotifySubscribers(in ParameterViewLifetime lifetime)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.NotifyCascadingValueChanged(lifetime);
            }
        }

        private void Render(RenderTreeBuilder builder)
        {
            builder.AddContent(0, ChildContent);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// A builder for producing a mapping of keys to <see cref="IDispatcherValueConstraint"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DispatcherValueConstraintBuilder"/> allows iterative building a set of dispatcher value constraints, and will
    /// merge multiple entries for the same key.
    /// </remarks>
    public class DispatcherValueConstraintBuilder
    {
        private readonly IConstraintFactory _constraintFactory;
        private readonly string _rawText;
        private readonly Dictionary<string, List<IDispatcherValueConstraint>> _constraints;
        private readonly HashSet<string> _optionalParameters;

        /// <summary>
        /// Creates a new <see cref="DispatcherValueConstraintBuilder"/> instance.
        /// </summary>
        /// <param name="constraintFactory">The <see cref="IConstraintFactory"/>.</param>
        /// <param name="rawText">The display name (for use in error messages).</param>
        public DispatcherValueConstraintBuilder(
            IConstraintFactory constraintFactory,
            string rawText)
        {
            if (constraintFactory == null)
            {
                throw new ArgumentNullException(nameof(constraintFactory));
            }

            if (rawText == null)
            {
                throw new ArgumentNullException(nameof(rawText));
            }

            _constraintFactory = constraintFactory;
            _rawText = rawText;

            _constraints = new Dictionary<string, List<IDispatcherValueConstraint>>(StringComparer.OrdinalIgnoreCase);
            _optionalParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Builds a mapping of constraints.
        /// </summary>
        /// <returns>An <see cref="IDictionary{String, IDispatcherValueConstraint}"/> of the constraints.</returns>
        public IDictionary<string, IDispatcherValueConstraint> Build()
        {
            var constraints = new Dictionary<string, IDispatcherValueConstraint>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _constraints)
            {
                IDispatcherValueConstraint constraint;
                if (kvp.Value.Count == 1)
                {
                    constraint = kvp.Value[0];
                }
                else
                {
                    constraint = new CompositeDispatcherValueConstraint(kvp.Value.ToArray());
                }

                if (_optionalParameters.Contains(kvp.Key))
                {
                    var optionalConstraint = new OptionalDispatcherValueConstraint(constraint);
                    constraints.Add(kvp.Key, optionalConstraint);
                }
                else
                {
                    constraints.Add(kvp.Key, constraint);
                }
            }

            return constraints;
        }

        /// <summary>
        /// Adds a constraint instance for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">
        /// The constraint instance. Must either be a string or an instance of <see cref="IDispatcherValueConstraint"/>.
        /// </param>
        /// <remarks>
        /// If the <paramref name="value"/> is a string, it will be converted to a <see cref="RegexDispatcherValueConstraint"/>.
        ///
        /// For example, the string <code>Product[0-9]+</code> will be converted to the regular expression
        /// <code>^(Product[0-9]+)</code>. See <see cref="System.Text.RegularExpressions.Regex"/> for more details.
        /// </remarks>
        public void AddConstraint(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var constraint = value as IDispatcherValueConstraint;
            if (constraint == null)
            {
                var regexPattern = value as string;
                if (regexPattern == null)
                {
                    throw new InvalidOperationException(
                        Resources.FormatDispatcherValueConstraintBuilder_ValidationMustBeStringOrCustomConstraint(
                            key,
                            value,
                            _rawText,
                            typeof(IDispatcherValueConstraint)));
                }

                var constraintsRegEx = "^(" + regexPattern + ")$";
                constraint = new RegexDispatcherValueConstraint(constraintsRegEx);
            }

            Add(key, constraint);
        }

        /// <summary>
        /// Adds a constraint for the given key, resolved by the <see cref="IConstraintFactory"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="constraintText">The text to be resolved by <see cref="IConstraintFactory"/>.</param>
        /// <remarks>
        /// The <see cref="IConstraintFactory"/> can create <see cref="IDispatcherValueConstraint"/> instances
        /// based on <paramref name="constraintText"/>. See <see cref="DispatcherOptions.ConstraintMap"/> to register
        /// custom constraint types.
        /// </remarks>
        public void AddResolvedConstraint(string key, string constraintText)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (constraintText == null)
            {
                throw new ArgumentNullException(nameof(constraintText));
            }

            var constraint = _constraintFactory.ResolveConstraint(constraintText);
            if (constraint == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatDispatcherValueConstraintBuilder_CouldNotResolveConstraint(
                        key,
                        constraintText,
                        _rawText,
                        _constraintFactory.GetType().Name));
            }

            Add(key, constraint);
        }

        /// <summary>
        /// Sets the given key as optional.
        /// </summary>
        /// <param name="key">The key.</param>
        public void SetOptional(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _optionalParameters.Add(key);
        }

        private void Add(string key, IDispatcherValueConstraint constraint)
        {
            if (!_constraints.TryGetValue(key, out var list))
            {
                list = new List<IDispatcherValueConstraint>();
                _constraints.Add(key, list);
            }

            list.Add(constraint);
        }
    }
}

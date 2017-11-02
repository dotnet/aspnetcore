// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// The default implementation of <see cref="IConstraintFactory"/>. Resolves constraints by parsing
    /// a constraint key and constraint arguments, using a map to resolve the constraint type, and calling an
    /// appropriate constructor for the constraint type.
    /// </summary>
    public class DefaultConstraintFactory : IConstraintFactory
    {
        private readonly IDictionary<string, Type> _constraintMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConstraintFactory"/> class.
        /// </summary>
        /// <param name="dispatcherOptions">
        /// Accessor for <see cref="DispatcherOptions"/> containing the constraints of interest.
        /// </param>
        public DefaultConstraintFactory(IOptions<DispatcherOptions> dispatcherOptions)
        {
            _constraintMap = dispatcherOptions.Value.ConstraintMap;
        }

        /// <inheritdoc />
        /// <example>
        /// A typical constraint looks like the following
        /// "exampleConstraint(arg1, arg2, 12)".
        /// Here if the type registered for exampleConstraint has a single constructor with one argument,
        /// The entire string "arg1, arg2, 12" will be treated as a single argument.
        /// In all other cases arguments are split at comma.
        /// </example>
        public virtual IDispatcherValueConstraint ResolveConstraint(string constraint)
        {
            if (constraint == null)
            {
                throw new ArgumentNullException(nameof(constraint));
            }

            string constraintKey;
            string argumentString;
            var indexOfFirstOpenParens = constraint.IndexOf('(');
            if (indexOfFirstOpenParens >= 0 && constraint.EndsWith(")", StringComparison.Ordinal))
            {
                constraintKey = constraint.Substring(0, indexOfFirstOpenParens);
                argumentString = constraint.Substring(indexOfFirstOpenParens + 1,
                                                            constraint.Length - indexOfFirstOpenParens - 2);
            }
            else
            {
                constraintKey = constraint;
                argumentString = null;
            }

            if (!_constraintMap.TryGetValue(constraintKey, out var constraintType))
            {
                // Cannot resolve the constraint key
                return null;
            }

            if (!typeof(IDispatcherValueConstraint).GetTypeInfo().IsAssignableFrom(constraintType.GetTypeInfo()))
            {
                throw new InvalidOperationException(
                            Resources.FormatDefaultConstraintResolver_TypeNotConstraint(
                                                        constraintType, constraintKey, typeof(IDispatcherValueConstraint).Name));
            }

            try
            {
                return CreateConstraint(constraintType, argumentString);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"An error occurred while trying to create an instance of route constraint '{constraintType.FullName}'.",
                    exception);
            }
        }

        private static IDispatcherValueConstraint CreateConstraint(Type constraintType, string argumentString)
        {
            // No arguments - call the default constructor
            if (argumentString == null)
            {
                return (IDispatcherValueConstraint)Activator.CreateInstance(constraintType);
            }

            var constraintTypeInfo = constraintType.GetTypeInfo();
            ConstructorInfo activationConstructor = null;
            object[] parameters = null;
            var constructors = constraintTypeInfo.DeclaredConstructors.ToArray();

            // If there is only one constructor and it has a single parameter, pass the argument string directly
            // This is necessary for the RegexDispatcherValueConstraint to ensure that patterns are not split on commas.
            if (constructors.Length == 1 && constructors[0].GetParameters().Length == 1)
            {
                activationConstructor = constructors[0];
                parameters = ConvertArguments(activationConstructor.GetParameters(), new string[] { argumentString });
            }
            else
            {
                var arguments = argumentString.Split(',').Select(argument => argument.Trim()).ToArray();

                var matchingConstructors = constructors.Where(ci => ci.GetParameters().Length == arguments.Length)
                                                       .ToArray();
                var constructorMatches = matchingConstructors.Length;

                if (constructorMatches == 0)
                {
                    throw new InvalidOperationException(
                                Resources.FormatDefaultConstraintResolver_CouldNotFindCtor(
                                                       constraintTypeInfo.Name, arguments.Length));
                }
                else if (constructorMatches == 1)
                {
                    activationConstructor = matchingConstructors[0];
                    parameters = ConvertArguments(activationConstructor.GetParameters(), arguments);
                }
                else
                {
                    throw new InvalidOperationException(
                                Resources.FormatDefaultConstraintResolver_AmbiguousCtors(
                                                       constraintTypeInfo.Name, arguments.Length));
                }
            }

            return (IDispatcherValueConstraint)activationConstructor.Invoke(parameters);
        }

        private static object[] ConvertArguments(ParameterInfo[] parameterInfos, string[] arguments)
        {
            var parameters = new object[parameterInfos.Length];
            for (var i = 0; i < parameterInfos.Length; i++)
            {
                var parameter = parameterInfos[i];
                var parameterType = parameter.ParameterType;
                parameters[i] = Convert.ChangeType(arguments[i], parameterType, CultureInfo.InvariantCulture);
            }

            return parameters;
        }
    }
}


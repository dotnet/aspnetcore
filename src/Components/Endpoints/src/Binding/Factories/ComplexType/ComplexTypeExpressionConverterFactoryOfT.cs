// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Components.Endpoints.Binding;

internal sealed class ComplexTypeExpressionConverterFactory<T> : ComplexTypeExpressionConverterFactory
{
    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    internal override CompiledComplexTypeConverter<T> CreateConverter(Type type, FormDataMapperOptions options)
    {
        var body = CreateConverterBody(type, options);
        return new CompiledComplexTypeConverter<T>(body);
    }

    [RequiresDynamicCode(FormBindingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormBindingHelpers.RequiresUnreferencedCodeMessage)]
    private CompiledComplexTypeConverter<T>.ConverterDelegate CreateConverterBody(Type type, FormDataMapperOptions options)
    {
        var properties = PropertyHelper.GetVisibleProperties(type);

        var (readerParam, typeParam, optionsParam, resultParam, foundValueParam) = CreateFormDataConverterParameters();
        var parameters = new List<ParameterExpression>() { readerParam, typeParam, optionsParam, resultParam, foundValueParam };

        // Variables
        var propertyFoundValue = Expression.Variable(typeof(bool), "foundValueForProperty");
        var succeeded = Expression.Variable(typeof(bool), "succeeded");
        var localFoundValueVar = Expression.Variable(typeof(bool), "localFoundValue");

        var variables = new List<ParameterExpression>() { propertyFoundValue, succeeded, localFoundValueVar };
        var propertyLocals = new List<ParameterExpression>();

        var body = new List<Expression>()
        {
            Expression.Assign(succeeded, Expression.Constant(true)),
        };

        // Create the property blocks

        // var propertyConverter = options.ResolveConverter(typeof(string));
        // reader.PushPrefix("Property");
        // succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
        // found ||= foundProperty;
        // reader.PopPrefix("Property");
        for (var i = 0; i < properties.Length; i++)
        {
            // Declare variable for the converter
            var property = properties[i].Property;
            var propertyConverterType = typeof(FormDataConverter<>).MakeGenericType(property.PropertyType);
            var propertyConverterVar = Expression.Variable(propertyConverterType, $"{property.Name}Converter");
            variables.Add(propertyConverterVar);

            // Declare variable for property value.
            var propertyVar = Expression.Variable(property.PropertyType, property.Name);
            propertyLocals.Add(propertyVar);

            // Resolve and assign converter

            // Create the block to try and map the property and update variables.
            // returnParam &= { PushPrefix(property.Name); var res = TryRead(...); PopPrefix(...); return res; }
            // var propertyConverter = options.ResolveConverter<TProperty>());
            var propertyConverter = Expression.Assign(
                propertyConverterVar,
                Expression.Call(
                    optionsParam,
                    nameof(FormDataMapperOptions.ResolveConverter),
                    new[] { property.PropertyType },
                    Array.Empty<Expression>()));
            body.Add(propertyConverter);

            // reader.PushPrefix("Property");
            body.Add(Expression.Call(
                readerParam,
                nameof(FormDataReader.PushPrefix),
                Array.Empty<Type>(),
                Expression.Constant(property.Name)));

            // succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
            var callTryRead = Expression.AndAssign(
                succeeded,
                Expression.Call(
                    propertyConverterVar,
                    nameof(FormDataConverter<T>.TryRead),
                    Type.EmptyTypes,
                    readerParam,
                    typeParam,
                    optionsParam,
                    propertyVar,
                    propertyFoundValue));
            body.Add(callTryRead);

            // reader.PopPrefix("Property");
            body.Add(Expression.Call(
                readerParam,
                nameof(FormDataReader.PopPrefix),
                Array.Empty<Type>(),
                Expression.Constant(property.Name)));

            body.Add(Expression.OrAssign(localFoundValueVar, propertyFoundValue));
        }

        body.Add(Expression.IfThen(
            localFoundValueVar,
            Expression.Block(CreateInstanceAndAssignProperties(type, resultParam, properties, propertyLocals))));

        // foundValue && !failures;

        body.Add(Expression.Assign(foundValueParam, localFoundValueVar));
        body.Add(succeeded);

        variables.AddRange(propertyLocals);

        return CreateConverterFunction(parameters, variables, body);

        static IEnumerable<Expression> CreateInstanceAndAssignProperties(
            Type model,
            ParameterExpression resultParam,
            PropertyHelper[] props,
            List<ParameterExpression> variables)
        {
            if (!model.IsValueType)
            {
                yield return Expression.Assign(resultParam, Expression.New(model));
            }

            for (var i = 0; i < props.Length; i++)
            {
                yield return Expression.Assign(Expression.Property(resultParam, props[i].Property), variables[i]);
            }
        }
    }

    private static CompiledComplexTypeConverter<T>.ConverterDelegate CreateConverterFunction(
        List<ParameterExpression> parameters,
        List<ParameterExpression> variables,
        List<Expression> body)
    {
        var lambda = Expression.Lambda<CompiledComplexTypeConverter<T>.ConverterDelegate>(
            Expression.Block(variables, body),
            parameters);

        return lambda.Compile();
    }

    private static FormDataConverterReadParameters CreateFormDataConverterParameters()
    {
        return new(
            Expression.Parameter(typeof(FormDataReader).MakeByRefType(), "reader"),
            Expression.Parameter(typeof(Type), "type"),
            Expression.Parameter(typeof(FormDataMapperOptions), "options"),
            Expression.Parameter(typeof(T).MakeByRefType(), "result"),
            Expression.Parameter(typeof(bool).MakeByRefType(), "foundValue"));
    }

    private readonly struct FormDataConverterReadParameters
    {
        public ParameterExpression ReaderParam { get; }

        public ParameterExpression TypeParam { get; }

        public ParameterExpression OptionsParam { get; }

        public ParameterExpression ResultParam { get; }

        public ParameterExpression FoundValueParam { get; }

        public FormDataConverterReadParameters(
            ParameterExpression readerParam,
            ParameterExpression typeParam,
            ParameterExpression optionsParam,
            ParameterExpression resultParam,
            ParameterExpression foundValueParam)
        {
            ReaderParam = readerParam;
            TypeParam = typeParam;
            OptionsParam = optionsParam;
            ResultParam = resultParam;
            FoundValueParam = foundValueParam;
        }

        public void Deconstruct(
            out ParameterExpression readerParam,
            out ParameterExpression typeParam,
            out ParameterExpression optionsParam,
            out ParameterExpression resultParam,
            out ParameterExpression foundValueParam)
        {
            readerParam = ReaderParam;
            typeParam = TypeParam;
            optionsParam = OptionsParam;
            resultParam = ResultParam;
            foundValueParam = FoundValueParam;
        }
    }
}

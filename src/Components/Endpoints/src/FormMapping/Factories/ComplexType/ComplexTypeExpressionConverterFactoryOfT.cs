// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping;

internal sealed class ComplexTypeExpressionConverterFactory<T>(FormDataMetadataFactory factory) : ComplexTypeExpressionConverterFactory
{
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    internal override CompiledComplexTypeConverter<T> CreateConverter(Type type, FormDataMapperOptions options)
    {
        var body = CreateConverterBody(type, options);
        return new CompiledComplexTypeConverter<T>(body);
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private CompiledComplexTypeConverter<T>.ConverterDelegate CreateConverterBody(Type type, FormDataMapperOptions options)
    {
        var metadata = factory.GetOrCreateMetadataFor(type, options);
        var properties = metadata.Properties;

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

        var end = Expression.Label("done");

        if (metadata.IsRecursive)
        {
            // For recursive types we need to do a prefix check.
            // if(!reader.CurrentPrefixExists())
            // {
            //     found = false;
            //     return true;
            // }
            //
            body.Add(
                Expression.IfThen(
                    Expression.Not(Expression.Call(readerParam, nameof(FormDataReader.CurrentPrefixExists), Array.Empty<Type>())),
                        Expression.Block(
                            Expression.Assign(foundValueParam, Expression.Constant(false)),
                            Expression.Assign(succeeded, Expression.Constant(true)),
                            Expression.Goto(end))));
        }

        // Create the property blocks

        // var propertyConverter = options.ResolveConverter(typeof(string));
        // reader.PushPrefix("PropertyInfo");
        // succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
        // found ||= foundProperty;
        // reader.PopPrefix("PropertyInfo");
        for (var i = 0; i < properties.Count; i++)
        {
            // Declare variable for the converter
            var property = properties[i].Property;
            var propertyConverterType = typeof(FormDataConverter<>).MakeGenericType(property.PropertyInfo.PropertyType);
            var propertyConverterVar = Expression.Variable(propertyConverterType, $"{property.Name}Converter");
            variables.Add(propertyConverterVar);

            // Declare variable for property value.
            var propertyVar = Expression.Variable(property.PropertyInfo.PropertyType, property.Name);
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
                    new[] { property.PropertyInfo.PropertyType },
                    Array.Empty<Expression>()));
            body.Add(propertyConverter);

            // try
            // {
            //     reader.PushPrefix("PropertyInfo");
            //     succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
            // }
            // finally
            // {
            //     reader.PopPrefix("PropertyInfo");
            // }
            body.Add(Expression.TryFinally(
                body: Expression.Block(
                    // reader.PushPrefix("PropertyInfo");
                    Expression.Call(
                        readerParam,
                        nameof(FormDataReader.PushPrefix),
                        Array.Empty<Type>(),
                        Expression.Constant(property.Name)),
                    // succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
                    Expression.AndAssign(
                        succeeded,
                        Expression.Call(
                            propertyConverterVar,
                            nameof(FormDataConverter<T>.TryRead),
                            Type.EmptyTypes,
                            readerParam,
                            Expression.Constant(property.PropertyInfo.PropertyType),
                            optionsParam,
                            propertyVar,
                            propertyFoundValue))),
                // reader.PopPrefix("PropertyInfo");
                @finally: Expression.Call(
                    readerParam,
                    nameof(FormDataReader.PopPrefix),
                    Array.Empty<Type>(),
                    Expression.Constant(property.Name))));

            body.Add(Expression.OrAssign(localFoundValueVar, propertyFoundValue));
        }

        body.Add(Expression.IfThen(
            localFoundValueVar,
            Expression.Block(CreateInstanceAndAssignProperties(type, resultParam, properties, propertyLocals, succeeded, readerParam))));

        // foundValue && !failures;

        body.Add(Expression.Assign(foundValueParam, localFoundValueVar));
        body.Add(Expression.Label(end));
        body.Add(succeeded);

        variables.AddRange(propertyLocals);

        return CreateConverterFunction(parameters, variables, body);

        static IEnumerable<Expression> CreateInstanceAndAssignProperties(
            Type model,
            ParameterExpression resultParam,
            IList<FormDataPropertyMetadata> props,
            List<ParameterExpression> variables,
            ParameterExpression succeeded,
            ParameterExpression context)
        {
            if (!model.IsValueType)
            {
                yield return Expression.Assign(resultParam, Expression.New(model));
            }

            // if(!succeeded && context.AttachInstanceToErrorsHandler != null)
            // {
            //     context.AttachInstanceToErrors((object)result);
            // }
            yield return Expression.IfThen(
                Expression.And(
                    Expression.Not(succeeded),
                    Expression.NotEqual(
                        Expression.Property(context, nameof(FormDataReader.AttachInstanceToErrorsHandler)),
                        Expression.Constant(null, typeof(Action<string, object>)))),
                    Expression.Call(
                        context,
                        nameof(FormDataReader.AttachInstanceToErrors),
                        Array.Empty<Type>(),
                        Expression.Convert(resultParam, typeof(object))));

            for (var i = 0; i < props.Count; i++)
            {
                yield return Expression.Assign(Expression.Property(resultParam, props[i].Property.PropertyInfo), variables[i]);
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

    private record struct FormDataConverterReadParameters(
        ParameterExpression ReaderParam,
        ParameterExpression TypeParam,
        ParameterExpression OptionsParam,
        ParameterExpression ResultParam,
        ParameterExpression FoundValueParam);
}

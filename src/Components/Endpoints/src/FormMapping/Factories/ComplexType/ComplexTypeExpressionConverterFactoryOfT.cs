// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
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
        var constructorParameters = metadata.ConstructorParameters;

        var (readerParam, typeParam, optionsParam, resultParam, foundValueParam) = CreateFormDataConverterParameters();
        var parameters = new List<ParameterExpression>() { readerParam, typeParam, optionsParam, resultParam, foundValueParam };

        // Variables
        var propertyFoundValue = Expression.Variable(typeof(bool), "foundValueForProperty");
        var succeeded = Expression.Variable(typeof(bool), "succeeded");
        var localFoundValueVar = Expression.Variable(typeof(bool), "localFoundValue");

        var variables = new List<ParameterExpression>() { propertyFoundValue, succeeded, localFoundValueVar };
        var propertyValueLocals = new List<ParameterExpression>();
        var constructorParameterValueLocals = new List<ParameterExpression>();

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

        // Create the constructor parameter blocks

        // var parameterConverter = options.ResolveConverter(typeof(string));
        // reader.PushPrefix("PropertyInfo");
        // succeeded &= parameterConverter.TryRead(ref reader, typeof(string), options, out constructorParameterVar, out foundProperty);
        // found ||= foundProperty;
        // reader.PopPrefix("PropertyInfo");
        for (var i = 0; i < constructorParameters.Count; i++)
        {
            // Declare variable for the converter
            var constructorParameter = constructorParameters[i];
            var constructorParameterConverterType = typeof(FormDataConverter<>).MakeGenericType(constructorParameter.Type);
            var constructorParameterConverterVar = Expression.Variable(constructorParameterConverterType, $"{constructorParameter.Name}Converter");
            variables.Add(constructorParameterConverterVar);

            // Declare variable for constructorParameter value.
            var constructorParameterVar = Expression.Variable(constructorParameter.Type, constructorParameter.Name);
            constructorParameterValueLocals.Add(constructorParameterVar);

            // Resolve and assign converter
            // Create the block to try and map the constructorParameter and update propsLocals.
            // returnParam &= { PushPrefix(constructorParameter.Name); var res = TryRead(...); PopPrefix(...); return res; }
            // var constructorParameterConverter = options.ResolveConverter<TProperty>());
            var constructorParameterConverter = Expression.Assign(
                constructorParameterConverterVar,
                Expression.Call(
                    optionsParam,
                    nameof(FormDataMapperOptions.ResolveConverter),
                    new[] { constructorParameter.Type },
                    Array.Empty<Expression>()));
            body.Add(constructorParameterConverter);

            // try
            // {
            //     reader.PushPrefix("PropertyInfo");
            //     succeeded &= constructorParameterConverter.TryRead(ref reader, typeof(string), options, out constructorParameterVar, out foundProperty);
            //     if(!succeeded || !found)
            //     {
            //         reader.AddMappingError("Missing required value for constructor parameter {0}", constructorParameter.Name);
            //     }
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
                        Expression.Constant(constructorParameter.Name)),
                    // succeeded &= constructorParameterConverter.TryRead(ref reader, typeof(string), options, out constructorParameterVar, out foundProperty);
                    Expression.AndAssign(
                        succeeded,
                        Expression.Call(
                            constructorParameterConverterVar,
                            nameof(FormDataConverter<T>.TryRead),
                            Type.EmptyTypes,
                            readerParam,
                            Expression.Constant(constructorParameter.Type),
                            optionsParam,
                            constructorParameterVar,
                            propertyFoundValue)),
                    //     if(!found)
                    //     {
                    //         reader.AddMappingError("Missing required value for constructor parameter {0}", constructorParameter.Name);
                    //     }
                    Expression.IfThen(Expression.Not(propertyFoundValue),
                        Expression.Call(
                            readerParam,
                            nameof(FormDataReader.AddMappingError),
                            Array.Empty<Type>(),
                            // FormattableStringFactory.Create("Missing required value for constructor parameter '{0}'.", constructorParameter.Name)
                            Expression.Call(
                                typeof(FormattableStringFactory),
                                nameof(FormattableStringFactory.Create),
                                Array.Empty<Type>(),
                                Expression.Constant("Missing required value for constructor parameter '{0}'."),
                                Expression.NewArrayInit(typeof(object), Expression.Constant(constructorParameter.Name, typeof(string)))),
                            Expression.Constant(null, typeof(string))))
                    ),
                // reader.PopPrefix("PropertyInfo");
                @finally: Expression.Call(
                    readerParam,
                    nameof(FormDataReader.PopPrefix),
                    Array.Empty<Type>(),
                    Expression.Constant(constructorParameter.Name))));

            body.Add(Expression.OrAssign(localFoundValueVar, propertyFoundValue));
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
            var property = properties[i];
            var propertyConverterType = typeof(FormDataConverter<>).MakeGenericType(property.Type);
            var propertyConverterVar = Expression.Variable(propertyConverterType, $"{property.Name}Converter");
            variables.Add(propertyConverterVar);

            // Declare variable for property value.
            var propertyVar = Expression.Variable(property.Type, property.Name);
            propertyValueLocals.Add(propertyVar);

            // Resolve and assign converter
            // Create the block to try and map the property and update propsLocals.
            // returnParam &= { PushPrefix(property.Name); var res = TryRead(...); PopPrefix(...); return res; }
            // var propertyConverter = options.ResolveConverter<TProperty>());
            var propertyConverter = Expression.Assign(
                propertyConverterVar,
                Expression.Call(
                    optionsParam,
                    nameof(FormDataMapperOptions.ResolveConverter),
                    new[] { property.Type },
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

            var propertyBody = new List<Expression>
            {
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
                        Expression.Constant(property.Type),
                        optionsParam,
                        propertyVar,
                        propertyFoundValue))
            };

            if (property.Required)
            {
                //     if(!found)
                //     {
                //         reader.AddMappingError("Missing required value for constructor parameter {0}", constructorParameter.Name);
                //     }
                Expression.IfThen(Expression.Not(propertyFoundValue),
                    Expression.Call(
                        readerParam,
                        nameof(FormDataReader.AddMappingError),
                        Array.Empty<Type>(),
                        // FormattableStringFactory.Create("Missing required value for property parameter '{0}'.", property.Name)
                        Expression.Call(
                            typeof(FormattableStringFactory),
                            nameof(FormattableStringFactory.Create),
                            Array.Empty<Type>(),
                            Expression.Constant("Missing required value for property parameter '{0}'."),
                            Expression.NewArrayInit(typeof(object), Expression.Constant(property.Name, typeof(string)))),
                        Expression.Constant(null, typeof(string))));
            }

            body.Add(Expression.TryFinally(
                body: Expression.Block(propertyBody),
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
            Expression.Block(CreateInstanceAndAssignProperties(
                metadata,
                resultParam,
                constructorParameters,
                constructorParameterValueLocals,
                properties,
                propertyValueLocals,
                variables,
                succeeded,
                readerParam))));

        // foundValue && !failures;

        body.Add(Expression.Assign(foundValueParam, localFoundValueVar));
        body.Add(Expression.Label(end));
        body.Add(succeeded);

        variables.AddRange(constructorParameterValueLocals);
        variables.AddRange(propertyValueLocals);

        return CreateConverterFunction(parameters, variables, body);

        static IEnumerable<Expression> CreateInstanceAndAssignProperties(
            FormDataTypeMetadata model,
            ParameterExpression resultParam,
            IList<FormDataParameterInfo> constructorParameters,
            List<ParameterExpression> constructorParameterValueLocals,
            IList<FormDataPropertyMetadata> props,
            List<ParameterExpression> propsLocals,
            List<ParameterExpression> variables,
            ParameterExpression succeeded,
            ParameterExpression context)
        {
            if (model.Constructor == null && !model.Type.IsValueType)
            {
                throw new InvalidOperationException($"Type '{model.Type}' does not have a constructor. " +
                    $"A single public constructor is required for mapping the type.");
            }

            if (model.Constructor != null)
            {
                // try
                // {
                //     result = new T(...);
                // }
                // catch(Exception ex)
                // {
                //     reader.AddMappingError(ex.Message);
                //     succeeded = false;
                // }
                var exception = Expression.Variable(typeof(Exception), "constructorException");
                variables.Add(exception);
                yield return Expression.TryCatch(
                    Expression.Assign(resultParam, Expression.New(model.Constructor, constructorParameterValueLocals)),
                    Expression.Catch(
                        exception,
                        Expression.Block(
                            Expression.Call(
                                context,
                                nameof(FormDataReader.AddMappingError),
                                Array.Empty<Type>(),
                                exception,
                                Expression.Constant(null, typeof(string))),
                            Expression.Assign(succeeded, Expression.Constant(false, typeof(bool))),
                            resultParam)));
            }

            // if(!succeeded && context.AttachInstanceToErrorsHandler != null && result != null)
            // {
            //     context.AttachInstanceToErrors((object)result);
            // }
            yield return Expression.IfThen(
                Expression.And(
                    Expression.And(
                        Expression.Not(succeeded),
                        Expression.NotEqual(
                            Expression.Property(context, nameof(FormDataReader.AttachInstanceToErrorsHandler)),
                            Expression.Constant(null, typeof(Action<string, object>)))),
                        Expression.NotEqual(resultParam, Expression.Constant(null, resultParam.Type))),
                Expression.Call(
                    context,
                    nameof(FormDataReader.AttachInstanceToErrors),
                    Array.Empty<Type>(),
                    Expression.Convert(resultParam, typeof(object))));

            if (!model.Type.IsValueType)
            {
                var assignments = new List<Expression>();
                for (var i = 0; i < props.Count; i++)
                {
                    assignments.Add(Expression.Assign(Expression.Property(resultParam, props[i].Property), propsLocals[i]));
                }

                // if(result != null)
                // {
                //     result.Property1 = property1;
                //     result.Property2 = property2;
                //     ...
                // }
                yield return Expression.IfThen(
                    Expression.NotEqual(resultParam, Expression.Constant(null, resultParam.Type)),
                    Expression.Block(assignments));
            }
            else
            {
                for (var i = 0; i < props.Count; i++)
                {
                    yield return Expression.Assign(Expression.Property(resultParam, props[i].Property), propsLocals[i]);
                }
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

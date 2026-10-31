// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
        var metadata = factory.GetOrCreateMetadataFor(type, options) ??
            throw new InvalidOperationException($"Could not resolve metadata for type '{type.FullName}'.");

        var properties = metadata.Properties;
        var constructorParameters = metadata.ConstructorParameters;

        var (readerParam, typeParam, optionsParam, resultParam, foundValueParam) = CreateFormDataConverterParameters();
        var parameters = new List<ParameterExpression>() { readerParam, typeParam, optionsParam, resultParam, foundValueParam };

        // Variables
        var propertyFoundValue = Expression.Variable(typeof(bool), "foundValueForProperty");
        var succeeded = Expression.Variable(typeof(bool), "succeeded");
        var localFoundValueVar = Expression.Variable(typeof(bool), "localFoundValue");
        var exceptionVar = Expression.Variable(typeof(Exception), "mappingException");
        var variables = new List<ParameterExpression>() { propertyFoundValue, succeeded, localFoundValueVar, exceptionVar };

        var propertyValueLocals = new List<ParameterExpression>();
        var constructorParameterValueLocals = new List<ParameterExpression>();

        var body = new List<Expression>()
        {
            Expression.Assign(succeeded, Expression.Constant(true)),
        };

        var end = Expression.Label("done");

        if (metadata.IsRecursive)
        {
            body.Add(CreatePrefixCheckForRecursiveTypes(readerParam, foundValueParam, succeeded, end));
        }

        MapConstructorParameters(
            constructorParameters,
            readerParam,
            optionsParam,
            propertyFoundValue,
            succeeded,
            localFoundValueVar,
            exceptionVar,
            variables,
            constructorParameterValueLocals,
            body);

        MapPropertyValues(
            properties,
            readerParam,
            optionsParam,
            propertyFoundValue,
            succeeded,
            localFoundValueVar,
            exceptionVar,
            variables,
            propertyValueLocals,
            body);

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

    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static IEnumerable<Expression> CreateInstanceAndAssignProperties(
        FormDataTypeMetadata model,
        ParameterExpression resultParam,
        IList<FormDataParameterMetadata> constructorParameters,
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

        // If we got here it means that we found some values for the type.
        // We need to check the required properties/constructor parameters to see if we have values for them.
        // If we don't, we need to add a mapping error and set succeeded to false.
        var checks = ReportMissingValues(context, constructorParameterValueLocals, constructorParameters, propsLocals, props, succeeded);
        foreach (var missingValueCheck in checks)
        {
            yield return missingValueCheck;
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
                Expression.Assign(
                    resultParam,
                    Expression.New(
                        model.Constructor,
                        constructorParameterValueLocals.Select(GetValueLocalVariableValueExpression))),
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

        var failedAndHasHandler = Expression.And(Expression.Not(succeeded), HasHandler(context));

        var clause = model.Type.IsValueType ? failedAndHasHandler :
            Expression.And(
                failedAndHasHandler,
                Expression.NotEqual(
                    resultParam,
                    Expression.Constant(null, resultParam.Type)));

        yield return Expression.IfThen(
            clause,
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
                assignments.Add(Expression.Assign(Expression.Property(resultParam, props[i].Property), GetValueLocalVariableValueExpression(propsLocals[i])));
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
                yield return Expression.Assign(Expression.Property(resultParam, props[i].Property), GetValueLocalVariableValueExpression(propsLocals[i]));
            }
        }

        static BinaryExpression HasHandler(ParameterExpression context)
        {
            return Expression.NotEqual(
                                    Expression.Property(context, nameof(FormDataReader.AttachInstanceToErrorsHandler)),
                                    Expression.Constant(null, typeof(Action<string, object>)));
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static IEnumerable<Expression> ReportMissingValues(
        Expression readerParam,
        List<ParameterExpression> constructorParameters,
        IList<FormDataParameterMetadata> constructorParameterMetadata,
        List<ParameterExpression> properties,
        IList<FormDataPropertyMetadata> propertyMetadata,
        ParameterExpression succeeded)
    {
        for (var i = 0; i < constructorParameters.Count; i++)
        {
            var parameter = constructorParameters[i];
            var metadata = constructorParameterMetadata[i];
            if (metadata.Required)
            {
                // if(!property.Item1)
                // {
                //     reader.PushPrefix(metadata.Name);
                //     reader.AddMappingError(
                //         FommattableStringFactory.Create(
                //             "Missing required value for constructor property '{0}'.",
                //             metadata.Name));
                //     reader.PopPrefix(metadata.Name);
                // }
                yield return Expression.IfThen(
                    Expression.Not(GetValueLocalVariableFoundExpression(parameter)),
                    Expression.Block(
                        PushPrefix(readerParam, metadata.Name),
                        AddMappingError(readerParam, "Missing required value for constructor parameter '{0}'.", metadata.Name),
                        PopPrefix(readerParam, metadata.Name),
                        Expression.Assign(succeeded, Expression.Constant(false, typeof(bool)))));
            }
        }

        for (var i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var metadata = propertyMetadata[i];
            if (metadata.Required)
            {
                // if(!property.Item1)
                // {
                //     reader.PushPrefix(metadata.Name);
                //     reader.AddMappingError(
                //         FommattableStringFactory.Create(
                //             "Missing required value for constructor property '{0}'.",
                //             metadata.Name));
                //     reader.PopPrefix(metadata.Name);
                // }
                yield return Expression.IfThen(
                    Expression.Not(GetValueLocalVariableFoundExpression(property)),
                    Expression.Block(
                        PushPrefix(readerParam, metadata.Name),
                        AddMappingError(readerParam, "Missing required value for property '{0}'.", metadata.Name),
                        PopPrefix(readerParam, metadata.Name),
                        Expression.Assign(succeeded, Expression.Constant(false, typeof(bool)))));
            }
        }

        static MethodCallExpression PushPrefix(Expression readerParam, string prefix)
        {
            return Expression.Call(
                readerParam,
                nameof(FormDataReader.PushPrefix),
                Array.Empty<Type>(),
                Expression.Constant(prefix));
        }

        static MethodCallExpression AddMappingError(Expression readerParam, string message, string parameter)
        {
            // FormattableStringFactory.Create(message)
            var formattableString = Expression.Call(
                typeof(FormattableStringFactory),
                nameof(FormattableStringFactory.Create),
                Array.Empty<Type>(),
                Expression.Constant(message),
                Expression.NewArrayInit(typeof(object), Expression.Constant(parameter)));

            return Expression.Call(
                readerParam,
                nameof(FormDataReader.AddMappingError),
                Array.Empty<Type>(),
                formattableString,
                Expression.Constant(null, typeof(string)));
        }

        static MethodCallExpression PopPrefix(Expression readerParam, string prefix)
        {
            return Expression.Call(
                readerParam,
                nameof(FormDataReader.PopPrefix),
                Array.Empty<Type>(),
                Expression.Constant(prefix));
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static void MapPropertyValues(
        IList<FormDataPropertyMetadata> properties,
        ParameterExpression readerParam,
        ParameterExpression optionsParam,
        ParameterExpression propertyFoundValue,
        ParameterExpression succeeded,
        ParameterExpression localFoundValueVar,
        ParameterExpression exceptionVar,
        List<ParameterExpression> variables,
        List<ParameterExpression> propertyValueLocals,
        List<Expression> body)
    {
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
            var propertyVar = CreateValueLocalVariable(property);
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

            body.Add(Expression.TryCatchFinally(
                // try
                // {
                //     reader.PushPrefix("PropertyInfo");
                //     succeeded &= propertyConverter.TryRead(ref reader, typeof(string), options, out propertyVar, out foundProperty);
                // }
                // finally
                // {
                //     reader.PopPrefix("PropertyInfo");
                // }
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
                            Expression.Constant(property.Type),
                            optionsParam,
                            GetValueLocalVariableValueExpression(propertyVar),
                            propertyFoundValue))),
                // reader.PopPrefix("PropertyInfo");
                @finally: Expression.Call(
                    readerParam,
                    nameof(FormDataReader.PopPrefix),
                    Array.Empty<Type>(),
                    Expression.Constant(property.Name)),
                handlers: Expression.Catch(
                    exceptionVar,
                    Expression.Block(
                        Expression.Call(
                            readerParam,
                            nameof(FormDataReader.AddMappingError),
                            Array.Empty<Type>(),
                            exceptionVar,
                            Expression.Constant(null, typeof(string))),
                        Expression.Assign(succeeded, Expression.Constant(false, typeof(bool)))
                    ))));

            // parameter.found = foundProperty;
            body.Add(Expression.Assign(GetValueLocalVariableFoundExpression(propertyVar), propertyFoundValue));
            body.Add(Expression.OrAssign(localFoundValueVar, propertyFoundValue));
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static void MapConstructorParameters(
        IList<FormDataParameterMetadata> constructorParameters,
        ParameterExpression readerParam,
        ParameterExpression optionsParam,
        ParameterExpression propertyFoundValue,
        ParameterExpression succeeded,
        ParameterExpression localFoundValueVar,
        ParameterExpression exceptionVar,
        List<ParameterExpression> variables,
        List<ParameterExpression> constructorParameterValueLocals,
        List<Expression> body)
    {
        // Create the constructor property blocks

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
            var constructorParameterVar = CreateValueLocalVariable(constructorParameter);
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
            //         reader.AddMappingError("Missing required value for constructor property {0}", constructorParameter.Name);
            //     }
            // }
            // finally
            // {
            //     reader.PopPrefix("PropertyInfo");
            // }
            body.Add(Expression.TryCatchFinally(
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
                            GetValueLocalVariableValueExpression(constructorParameterVar),
                            propertyFoundValue))),
                // reader.PopPrefix("PropertyInfo");
                @finally: Expression.Call(
                    readerParam,
                    nameof(FormDataReader.PopPrefix),
                    Array.Empty<Type>(),
                    Expression.Constant(constructorParameter.Name)),
                handlers: Expression.Catch(
                    exceptionVar,
                    Expression.Block(
                        Expression.Call(
                            readerParam,
                            nameof(FormDataReader.AddMappingError),
                            Array.Empty<Type>(),
                            exceptionVar,
                            Expression.Constant(null, typeof(string))),
                        Expression.Assign(succeeded, Expression.Constant(false, typeof(bool)))
                    ))));

            // parameter.found = foundProperty;
            body.Add(Expression.Assign(GetValueLocalVariableFoundExpression(constructorParameterVar), propertyFoundValue));
            body.Add(Expression.OrAssign(localFoundValueVar, propertyFoundValue));
        }
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static Expression GetValueLocalVariableFoundExpression(ParameterExpression constructorParameterVar)
    {
        return Expression.PropertyOrField(constructorParameterVar, nameof(ValueTuple<bool, object>.Item1));
    }

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static Expression GetValueLocalVariableValueExpression(ParameterExpression constructorParameterVar) =>
        Expression.PropertyOrField(constructorParameterVar, nameof(ValueTuple<bool, object>.Item2));

    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static ParameterExpression CreateValueLocalVariable(IFormDataValue constructorParameter)
    {
        return Expression.Variable(typeof(ValueTuple<,>).MakeGenericType(typeof(bool), constructorParameter.Type), constructorParameter.Name);
    }

    // For recursive types we need to do a prefix check.
    // if(!reader.CurrentPrefixExists())
    // {
    //     found = false;
    //     return true;
    // }
    //
    [RequiresDynamicCode(FormMappingHelpers.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(FormMappingHelpers.RequiresUnreferencedCodeMessage)]
    private static ConditionalExpression CreatePrefixCheckForRecursiveTypes(ParameterExpression readerParam, ParameterExpression foundValueParam, ParameterExpression succeeded, LabelTarget end)
    {
        return Expression.IfThen(
            Expression.Not(Expression.Call(readerParam, nameof(FormDataReader.CurrentPrefixExists), Array.Empty<Type>())),
                Expression.Block(
                    Expression.Assign(foundValueParam, Expression.Constant(false)),
                    Expression.Assign(succeeded, Expression.Constant(true)),
                    Expression.Goto(end)));
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

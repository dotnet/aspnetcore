// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal static class EndpointParameterExtensions
{
    public static ITypeSymbol UnwrapParameterType(this EndpointParameter parameter)
    {
        var handlerParameterType = parameter.Type;
        var bindAsyncReturnType = parameter.GetBindAsyncReturnType();

        // If we can't resolve a return type from the `BindAsync` method, then just use the handler return type.
        if (bindAsyncReturnType is null)
        {
            return parameter.Source == EndpointParameterSource.BindAsync ? handlerParameterType.UnwrapTypeSymbol(unwrapNullable: true) : handlerParameterType;
        }
        // If the parameter defined in the route handler and the return type of the BindAsync method are the same,
        // then we can use the handler parameter type as the return type of the BindAsync method.
        if (SymbolEqualityComparer.IncludeNullability.Equals(handlerParameterType, bindAsyncReturnType))
        {
            return handlerParameterType;
        }
        // If the route handler parameter type is a value type, then we use it as the return type of the BindAsync method.
        // Even if the BindAsync method returns a type with mismatched nullability, we need to be able to correctly handle
        // mapping nullable value types to non-nullable value types, as in the following example.
        //
        // public struct NullableStruct
        // {
        //    public static ValueTask<NullableStruct?> BindAsync(HttpContext context, ParameterInfo parameter) {}
        // }
        // app.MapPost("/", (NullableStruct foo) => { })
        if (handlerParameterType.IsValueType)
        {
            // For generic structs like Struct<T> we want to use the return type of the BindAsync method as the return type
            // to avoid issues with mapping the generic type parameter if it contains mismatched nullability.
            //
            // public struct Struct<T>
            // {
            //    public static ValueTask<Struct<T?>> BindAsync(HttpContext context, ParameterInfo parameter) {}
            // }
            if (handlerParameterType is INamedTypeSymbol { IsGenericType: true, OriginalDefinition: { SpecialType: not SpecialType.System_Nullable_T } })
            {
                return bindAsyncReturnType;
            }
            return handlerParameterType;
        }

        return bindAsyncReturnType;
    }

    public static ITypeSymbol? GetBindAsyncReturnType(this EndpointParameter parameter)
        => ((INamedTypeSymbol?)parameter.BindableMethodSymbol?.ReturnType)?.TypeArguments[0];

    public static bool IsNullableOfT(this ITypeSymbol? typeSymbol)
        => typeSymbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
}

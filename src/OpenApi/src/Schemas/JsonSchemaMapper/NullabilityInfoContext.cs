// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER
namespace System.Reflection
{
    /// <summary>
    /// Provides APIs for populating nullability information/context from reflection members:
    /// <see cref="ParameterInfo"/>, <see cref="FieldInfo"/>, <see cref="PropertyInfo"/> and <see cref="EventInfo"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class NullabilityInfoContext
    {
        private const string CompilerServicesNameSpace = "System.Runtime.CompilerServices";
        private readonly Dictionary<Module, NotAnnotatedStatus> _publicOnlyModules = new();
        private readonly Dictionary<MemberInfo, NullabilityState> _context = new();

        internal static bool IsSupported { get; } =
            AppContext.TryGetSwitch("System.Reflection.NullabilityInfoContext.IsSupported", out bool isSupported) ? isSupported : true;

        [Flags]
        private enum NotAnnotatedStatus
        {
            None = 0x0,    // no restriction, all members annotated
            Private = 0x1, // private members not annotated
            Internal = 0x2, // internal members not annotated
        }

        private NullabilityState? GetNullableContext(MemberInfo? memberInfo)
        {
            while (memberInfo != null)
            {
                if (_context.TryGetValue(memberInfo, out NullabilityState state))
                {
                    return state;
                }

                foreach (CustomAttributeData attribute in memberInfo.GetCustomAttributesData())
                {
                    if (attribute.AttributeType.Name == "NullableContextAttribute" &&
                        attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                        attribute.ConstructorArguments.Count == 1)
                    {
                        state = TranslateByte(attribute.ConstructorArguments[0].Value);
                        _context.Add(memberInfo, state);
                        return state;
                    }
                }

                memberInfo = memberInfo.DeclaringType;
            }

            return null;
        }

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="ParameterInfo" />.
        /// If the nullablePublicOnly feature is set for an assembly, like it does in .NET SDK, the private and/or internal member's
        /// nullability attributes are omitted, in this case the API will return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="parameterInfo">The parameter which nullability info gets populated.</param>
        /// <exception cref="ArgumentNullException">If the parameterInfo parameter is null.</exception>
        /// <returns><see cref="NullabilityInfo" />.</returns>
        public NullabilityInfo Create(ParameterInfo parameterInfo)
        {
            EnsureIsSupported();

            IList<CustomAttributeData> attributes = parameterInfo.GetCustomAttributesData();
            NullableAttributeStateParser parser = parameterInfo.Member is MethodBase method && IsPrivateOrInternalMethodAndAnnotationDisabled(method)
                ? NullableAttributeStateParser.Unknown
                : CreateParser(attributes);
            NullabilityInfo nullability = GetNullabilityInfo(parameterInfo.Member, parameterInfo.ParameterType, parser);

            if (nullability.ReadState != NullabilityState.Unknown)
            {
                CheckParameterMetadataType(parameterInfo, nullability);
            }

            CheckNullabilityAttributes(nullability, attributes);
            return nullability;
        }

        private void CheckParameterMetadataType(ParameterInfo parameter, NullabilityInfo nullability)
        {
            ParameterInfo? metaParameter;
            MemberInfo metaMember;

            switch (parameter.Member)
            {
                case ConstructorInfo ctor:
                    var metaCtor = (ConstructorInfo)GetMemberMetadataDefinition(ctor);
                    metaMember = metaCtor;
                    metaParameter = GetMetaParameter(metaCtor, parameter);
                    break;

                case MethodInfo method:
                    MethodInfo metaMethod = GetMethodMetadataDefinition(method);
                    metaMember = metaMethod;
                    metaParameter = string.IsNullOrEmpty(parameter.Name) ? metaMethod.ReturnParameter : GetMetaParameter(metaMethod, parameter);
                    break;

                default:
                    return;
            }

            if (metaParameter != null)
            {
                CheckGenericParameters(nullability, metaMember, metaParameter.ParameterType, parameter.Member.ReflectedType);
            }
        }

        private static ParameterInfo? GetMetaParameter(MethodBase metaMethod, ParameterInfo parameter)
        {
            var parameters = metaMethod.GetParameters();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameter.Position == i &&
                    parameter.Name == parameters[i].Name)
                {
                    return parameters[i];
                }
            }

            return null;
        }

        private static MethodInfo GetMethodMetadataDefinition(MethodInfo method)
        {
            if (method.IsGenericMethod && !method.IsGenericMethodDefinition)
            {
                method = method.GetGenericMethodDefinition();
            }

            return (MethodInfo)GetMemberMetadataDefinition(method);
        }

        private static void CheckNullabilityAttributes(NullabilityInfo nullability, IList<CustomAttributeData> attributes)
        {
            var codeAnalysisReadState = NullabilityState.Unknown;
            var codeAnalysisWriteState = NullabilityState.Unknown;

            foreach (CustomAttributeData attribute in attributes)
            {
                if (attribute.AttributeType.Namespace == "System.Diagnostics.CodeAnalysis")
                {
                    if (attribute.AttributeType.Name == "NotNullAttribute")
                    {
                        codeAnalysisReadState = NullabilityState.NotNull;
                    }
                    else if ((attribute.AttributeType.Name == "MaybeNullAttribute" ||
                            attribute.AttributeType.Name == "MaybeNullWhenAttribute") &&
                            codeAnalysisReadState == NullabilityState.Unknown &&
                            !IsValueTypeOrValueTypeByRef(nullability.Type))
                    {
                        codeAnalysisReadState = NullabilityState.Nullable;
                    }
                    else if (attribute.AttributeType.Name == "DisallowNullAttribute")
                    {
                        codeAnalysisWriteState = NullabilityState.NotNull;
                    }
                    else if (attribute.AttributeType.Name == "AllowNullAttribute" &&
                        codeAnalysisWriteState == NullabilityState.Unknown &&
                        !IsValueTypeOrValueTypeByRef(nullability.Type))
                    {
                        codeAnalysisWriteState = NullabilityState.Nullable;
                    }
                }
            }

            if (codeAnalysisReadState != NullabilityState.Unknown)
            {
                nullability.ReadState = codeAnalysisReadState;
            }

            if (codeAnalysisWriteState != NullabilityState.Unknown)
            {
                nullability.WriteState = codeAnalysisWriteState;
            }
        }

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="PropertyInfo" />.
        /// If the nullablePublicOnly feature is set for an assembly, like it does in .NET SDK, the private and/or internal member's
        /// nullability attributes are omitted, in this case the API will return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="propertyInfo">The parameter which nullability info gets populated.</param>
        /// <exception cref="ArgumentNullException">If the propertyInfo parameter is null.</exception>
        /// <returns><see cref="NullabilityInfo" />.</returns>
        public NullabilityInfo Create(PropertyInfo propertyInfo)
        {
            EnsureIsSupported();

            MethodInfo? getter = propertyInfo.GetGetMethod(true);
            MethodInfo? setter = propertyInfo.GetSetMethod(true);
            bool annotationsDisabled = (getter == null || IsPrivateOrInternalMethodAndAnnotationDisabled(getter))
                && (setter == null || IsPrivateOrInternalMethodAndAnnotationDisabled(setter));
            NullableAttributeStateParser parser = annotationsDisabled ? NullableAttributeStateParser.Unknown : CreateParser(propertyInfo.GetCustomAttributesData());
            NullabilityInfo nullability = GetNullabilityInfo(propertyInfo, propertyInfo.PropertyType, parser);

            if (getter != null)
            {
                CheckNullabilityAttributes(nullability, getter.ReturnParameter.GetCustomAttributesData());
            }
            else
            {
                nullability.ReadState = NullabilityState.Unknown;
            }

            if (setter != null)
            {
                CheckNullabilityAttributes(nullability, setter.GetParameters().Last().GetCustomAttributesData());
            }
            else
            {
                nullability.WriteState = NullabilityState.Unknown;
            }

            return nullability;
        }

        private bool IsPrivateOrInternalMethodAndAnnotationDisabled(MethodBase method)
        {
            if ((method.IsPrivate || method.IsFamilyAndAssembly || method.IsAssembly) &&
               IsPublicOnly(method.IsPrivate, method.IsFamilyAndAssembly, method.IsAssembly, method.Module))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="EventInfo" />.
        /// If the nullablePublicOnly feature is set for an assembly, like it does in .NET SDK, the private and/or internal member's
        /// nullability attributes are omitted, in this case the API will return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="eventInfo">The parameter which nullability info gets populated.</param>
        /// <exception cref="ArgumentNullException">If the eventInfo parameter is null.</exception>
        /// <returns><see cref="NullabilityInfo" />.</returns>
        public NullabilityInfo Create(EventInfo eventInfo)
        {
            EnsureIsSupported();

            return GetNullabilityInfo(eventInfo, eventInfo.EventHandlerType!, CreateParser(eventInfo.GetCustomAttributesData()));
        }

        /// <summary>
        /// Populates <see cref="NullabilityInfo" /> for the given <see cref="FieldInfo" />
        /// If the nullablePublicOnly feature is set for an assembly, like it does in .NET SDK, the private and/or internal member's
        /// nullability attributes are omitted, in this case the API will return NullabilityState.Unknown state.
        /// </summary>
        /// <param name="fieldInfo">The parameter which nullability info gets populated.</param>
        /// <exception cref="ArgumentNullException">If the fieldInfo parameter is null.</exception>
        /// <returns><see cref="NullabilityInfo" />.</returns>
        public NullabilityInfo Create(FieldInfo fieldInfo)
        {
            EnsureIsSupported();

            IList<CustomAttributeData> attributes = fieldInfo.GetCustomAttributesData();
            NullableAttributeStateParser parser = IsPrivateOrInternalFieldAndAnnotationDisabled(fieldInfo) ? NullableAttributeStateParser.Unknown : CreateParser(attributes);
            NullabilityInfo nullability = GetNullabilityInfo(fieldInfo, fieldInfo.FieldType, parser);
            CheckNullabilityAttributes(nullability, attributes);
            return nullability;
        }

        private static void EnsureIsSupported()
        {
            if (!IsSupported)
            {
                throw new InvalidOperationException("NullabilityInfoContext is not supported");
            }
        }

        private bool IsPrivateOrInternalFieldAndAnnotationDisabled(FieldInfo fieldInfo)
        {
            if ((fieldInfo.IsPrivate || fieldInfo.IsFamilyAndAssembly || fieldInfo.IsAssembly) &&
                IsPublicOnly(fieldInfo.IsPrivate, fieldInfo.IsFamilyAndAssembly, fieldInfo.IsAssembly, fieldInfo.Module))
            {
                return true;
            }

            return false;
        }

        private bool IsPublicOnly(bool isPrivate, bool isFamilyAndAssembly, bool isAssembly, Module module)
        {
            if (!_publicOnlyModules.TryGetValue(module, out NotAnnotatedStatus value))
            {
                value = PopulateAnnotationInfo(module.GetCustomAttributesData());
                _publicOnlyModules.Add(module, value);
            }

            if (value == NotAnnotatedStatus.None)
            {
                return false;
            }

            if (((isPrivate || isFamilyAndAssembly) && value.HasFlag(NotAnnotatedStatus.Private)) ||
                 (isAssembly && value.HasFlag(NotAnnotatedStatus.Internal)))
            {
                return true;
            }

            return false;
        }

        private static NotAnnotatedStatus PopulateAnnotationInfo(IList<CustomAttributeData> customAttributes)
        {
            foreach (CustomAttributeData attribute in customAttributes)
            {
                if (attribute.AttributeType.Name == "NullablePublicOnlyAttribute" &&
                    attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                    attribute.ConstructorArguments.Count == 1)
                {
                    if (attribute.ConstructorArguments[0].Value is bool boolValue && boolValue)
                    {
                        return NotAnnotatedStatus.Internal | NotAnnotatedStatus.Private;
                    }
                    else
                    {
                        return NotAnnotatedStatus.Private;
                    }
                }
            }

            return NotAnnotatedStatus.None;
        }

        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, NullableAttributeStateParser parser)
        {
            int index = 0;
            NullabilityInfo nullability = GetNullabilityInfo(memberInfo, type, parser, ref index);

            if (nullability.ReadState != NullabilityState.Unknown)
            {
                TryLoadGenericMetaTypeNullability(memberInfo, nullability);
            }

            return nullability;
        }

        private NullabilityInfo GetNullabilityInfo(MemberInfo memberInfo, Type type, NullableAttributeStateParser parser, ref int index)
        {
            NullabilityState state = NullabilityState.Unknown;
            NullabilityInfo? elementState = null;
            NullabilityInfo[] genericArgumentsState = Array.Empty<NullabilityInfo>();
            Type underlyingType = type;

            if (underlyingType.IsByRef || underlyingType.IsPointer)
            {
                underlyingType = underlyingType.GetElementType()!;
            }

            if (underlyingType.IsValueType)
            {
                if (Nullable.GetUnderlyingType(underlyingType) is { } nullableUnderlyingType)
                {
                    underlyingType = nullableUnderlyingType;
                    state = NullabilityState.Nullable;
                }
                else
                {
                    state = NullabilityState.NotNull;
                }

                if (underlyingType.IsGenericType)
                {
                    ++index;
                }
            }
            else
            {
                if (!parser.ParseNullableState(index++, ref state)
                    && GetNullableContext(memberInfo) is { } contextState)
                {
                    state = contextState;
                }

                if (underlyingType.IsArray)
                {
                    elementState = GetNullabilityInfo(memberInfo, underlyingType.GetElementType()!, parser, ref index);
                }
            }

            if (underlyingType.IsGenericType)
            {
                Type[] genericArguments = underlyingType.GetGenericArguments();
                genericArgumentsState = new NullabilityInfo[genericArguments.Length];

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    genericArgumentsState[i] = GetNullabilityInfo(memberInfo, genericArguments[i], parser, ref index);
                }
            }

            return new NullabilityInfo(type, state, state, elementState, genericArgumentsState);
        }

        private static NullableAttributeStateParser CreateParser(IList<CustomAttributeData> customAttributes)
        {
            foreach (CustomAttributeData attribute in customAttributes)
            {
                if (attribute.AttributeType.Name == "NullableAttribute" &&
                    attribute.AttributeType.Namespace == CompilerServicesNameSpace &&
                    attribute.ConstructorArguments.Count == 1)
                {
                    return new NullableAttributeStateParser(attribute.ConstructorArguments[0].Value);
                }
            }

            return new NullableAttributeStateParser(null);
        }

        private void TryLoadGenericMetaTypeNullability(MemberInfo memberInfo, NullabilityInfo nullability)
        {
            MemberInfo? metaMember = GetMemberMetadataDefinition(memberInfo);
            Type? metaType = null;
            if (metaMember is FieldInfo field)
            {
                metaType = field.FieldType;
            }
            else if (metaMember is PropertyInfo property)
            {
                metaType = GetPropertyMetaType(property);
            }

            if (metaType != null)
            {
                CheckGenericParameters(nullability, metaMember!, metaType, memberInfo.ReflectedType);
            }
        }

        private static MemberInfo GetMemberMetadataDefinition(MemberInfo member)
        {
            Type? type = member.DeclaringType;
            if ((type != null) && type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                return NullabilityInfoHelpers.GetMemberWithSameMetadataDefinitionAs(type.GetGenericTypeDefinition(), member);
            }

            return member;
        }

        private static Type GetPropertyMetaType(PropertyInfo property)
        {
            if (property.GetGetMethod(true) is MethodInfo method)
            {
                return method.ReturnType;
            }

            return property.GetSetMethod(true)!.GetParameters()[0].ParameterType;
        }

        private void CheckGenericParameters(NullabilityInfo nullability, MemberInfo metaMember, Type metaType, Type? reflectedType)
        {
            if (metaType.IsGenericParameter)
            {
                if (nullability.ReadState == NullabilityState.NotNull)
                {
                    TryUpdateGenericParameterNullability(nullability, metaType, reflectedType);
                }
            }
            else if (metaType.ContainsGenericParameters)
            {
                if (nullability.GenericTypeArguments.Length > 0)
                {
                    Type[] genericArguments = metaType.GetGenericArguments();

                    for (int i = 0; i < genericArguments.Length; i++)
                    {
                        CheckGenericParameters(nullability.GenericTypeArguments[i], metaMember, genericArguments[i], reflectedType);
                    }
                }
                else if (nullability.ElementType is { } elementNullability && metaType.IsArray)
                {
                    CheckGenericParameters(elementNullability, metaMember, metaType.GetElementType()!, reflectedType);
                }

                // We could also follow this branch for metaType.IsPointer, but since pointers must be unmanaged this
                // will be a no-op regardless
                else if (metaType.IsByRef)
                {
                    CheckGenericParameters(nullability, metaMember, metaType.GetElementType()!, reflectedType);
                }
            }
        }

        private bool TryUpdateGenericParameterNullability(NullabilityInfo nullability, Type genericParameter, Type? reflectedType)
        {
            Debug.Assert(genericParameter.IsGenericParameter);

            if (reflectedType is not null
                && !genericParameter.IsGenericMethodParameter()
                && TryUpdateGenericTypeParameterNullabilityFromReflectedType(nullability, genericParameter, reflectedType, reflectedType))
            {
                return true;
            }

            if (IsValueTypeOrValueTypeByRef(nullability.Type))
            {
                return true;
            }

            var state = NullabilityState.Unknown;
            if (CreateParser(genericParameter.GetCustomAttributesData()).ParseNullableState(0, ref state))
            {
                nullability.ReadState = state;
                nullability.WriteState = state;
                return true;
            }

            if (GetNullableContext(genericParameter) is { } contextState)
            {
                nullability.ReadState = contextState;
                nullability.WriteState = contextState;
                return true;
            }

            return false;
        }

        private bool TryUpdateGenericTypeParameterNullabilityFromReflectedType(NullabilityInfo nullability, Type genericParameter, Type context, Type reflectedType)
        {
            Debug.Assert(genericParameter.IsGenericParameter && !genericParameter.IsGenericMethodParameter());

            Type contextTypeDefinition = context.IsGenericType && !context.IsGenericTypeDefinition ? context.GetGenericTypeDefinition() : context;
            if (genericParameter.DeclaringType == contextTypeDefinition)
            {
                return false;
            }

            Type? baseType = contextTypeDefinition.BaseType;
            if (baseType is null)
            {
                return false;
            }

            if (!baseType.IsGenericType
                || (baseType.IsGenericTypeDefinition ? baseType : baseType.GetGenericTypeDefinition()) != genericParameter.DeclaringType)
            {
                return TryUpdateGenericTypeParameterNullabilityFromReflectedType(nullability, genericParameter, baseType, reflectedType);
            }

            Type[] genericArguments = baseType.GetGenericArguments();
            Type genericArgument = genericArguments[genericParameter.GenericParameterPosition];
            if (genericArgument.IsGenericParameter)
            {
                return TryUpdateGenericParameterNullability(nullability, genericArgument, reflectedType);
            }

            NullableAttributeStateParser parser = CreateParser(contextTypeDefinition.GetCustomAttributesData());
            int nullabilityStateIndex = 1; // start at 1 since index 0 is the type itself
            for (int i = 0; i < genericParameter.GenericParameterPosition; i++)
            {
                nullabilityStateIndex += CountNullabilityStates(genericArguments[i]);
            }

            return TryPopulateNullabilityInfo(nullability, parser, ref nullabilityStateIndex);

            static int CountNullabilityStates(Type type)
            {
                Type underlyingType = Nullable.GetUnderlyingType(type) ?? type;
                if (underlyingType.IsGenericType)
                {
                    int count = 1;
                    foreach (Type genericArgument in underlyingType.GetGenericArguments())
                    {
                        count += CountNullabilityStates(genericArgument);
                    }

                    return count;
                }

                if (underlyingType.HasElementType)
                {
                    return (underlyingType.IsArray ? 1 : 0) + CountNullabilityStates(underlyingType.GetElementType()!);
                }

                return type.IsValueType ? 0 : 1;
            }
        }

#pragma warning disable SA1204 // Static elements should appear before instance elements
        private static bool TryPopulateNullabilityInfo(NullabilityInfo nullability, NullableAttributeStateParser parser, ref int index)
#pragma warning restore SA1204 // Static elements should appear before instance elements
        {
            bool isValueType = IsValueTypeOrValueTypeByRef(nullability.Type);
            if (!isValueType)
            {
                var state = NullabilityState.Unknown;
                if (!parser.ParseNullableState(index, ref state))
                {
                    return false;
                }

                nullability.ReadState = state;
                nullability.WriteState = state;
            }

            if (!isValueType || (Nullable.GetUnderlyingType(nullability.Type) ?? nullability.Type).IsGenericType)
            {
                index++;
            }

            if (nullability.GenericTypeArguments.Length > 0)
            {
                foreach (NullabilityInfo genericTypeArgumentNullability in nullability.GenericTypeArguments)
                {
                    TryPopulateNullabilityInfo(genericTypeArgumentNullability, parser, ref index);
                }
            }
            else if (nullability.ElementType is { } elementTypeNullability)
            {
                TryPopulateNullabilityInfo(elementTypeNullability, parser, ref index);
            }

            return true;
        }

        private static NullabilityState TranslateByte(object? value)
        {
            return value is byte b ? TranslateByte(b) : NullabilityState.Unknown;
        }

        private static NullabilityState TranslateByte(byte b) =>
            b switch
            {
                1 => NullabilityState.NotNull,
                2 => NullabilityState.Nullable,
                _ => NullabilityState.Unknown
            };

        private static bool IsValueTypeOrValueTypeByRef(Type type) =>
            type.IsValueType || ((type.IsByRef || type.IsPointer) && type.GetElementType()!.IsValueType);

        private readonly struct NullableAttributeStateParser
        {
            private static readonly object UnknownByte = (byte)0;

            private readonly object? _nullableAttributeArgument;

            public NullableAttributeStateParser(object? nullableAttributeArgument)
            {
                this._nullableAttributeArgument = nullableAttributeArgument;
            }

            public static NullableAttributeStateParser Unknown => new(UnknownByte);

            public bool ParseNullableState(int index, ref NullabilityState state)
            {
                switch (this._nullableAttributeArgument)
                {
                    case byte b:
                        state = TranslateByte(b);
                        return true;
                    case ReadOnlyCollection<CustomAttributeTypedArgument> args
                        when index < args.Count && args[index].Value is byte elementB:
                        state = TranslateByte(elementB);
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
#endif

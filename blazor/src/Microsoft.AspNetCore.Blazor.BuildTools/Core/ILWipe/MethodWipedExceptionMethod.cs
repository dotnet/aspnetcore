// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe
{
    static class MethodWipedExceptionMethod
    {
        public static MethodDefinition AddToAssembly(ModuleDefinition moduleDefinition)
        {
            // Adds the following method to the assembly:
            // namespace ILWipe
            // {
            //     internal static class ILWipeHelpers
            //     {
            //         public static Exception CreateMethodWipedException()
            //         {
            //             return new NotImplementedException("Cannot call method because it was wiped. See stack trace for details.");
            //         }
            //     }
            // }
            var ilWipeHelpersType = new TypeDefinition("ILWipe", "ILWipeHelpers",
                TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                moduleDefinition.TypeSystem.Object);
            moduleDefinition.Types.Add(ilWipeHelpersType);

            var methodAttributes =
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.Static;
            var createMethodWipedExceptionMethod = new MethodDefinition(
                "CreateMethodWipedException",
                methodAttributes,
                ImportEquivalentTypeFromMscorlib(moduleDefinition, typeof(Exception)));
            ilWipeHelpersType.Methods.Add(createMethodWipedExceptionMethod);

            var notImplExceptionType = ImportEquivalentTypeFromMscorlib(moduleDefinition, typeof(NotImplementedException));
            var notImplExceptionCtor = new MethodReference(".ctor", moduleDefinition.TypeSystem.Void, notImplExceptionType);
            notImplExceptionCtor.HasThis = true;
            notImplExceptionCtor.Parameters.Add(new ParameterDefinition(moduleDefinition.TypeSystem.String));

            var il = createMethodWipedExceptionMethod.Body.GetILProcessor();
            il.Append(il.Create(OpCodes.Ldstr, "Cannot invoke method because it was wiped. See stack trace for details."));
            il.Append(il.Create(OpCodes.Newobj, notImplExceptionCtor));
            il.Append(il.Create(OpCodes.Ret));

            return createMethodWipedExceptionMethod;
        }

        static TypeReference ImportEquivalentTypeFromMscorlib(ModuleDefinition module, Type type)
        {
            // We have to do this instead of module.ImportReference(type), because the latter
            // would try to reference it in System.Private.CoreLib because this tool itself
            // compiles to target netcoreapp rather than netstandard
            IMetadataScope mscorlibScope;
            if (module.TryGetTypeReference(typeof(object).FullName, out var objectRef))
            {
                mscorlibScope = objectRef.Scope;
            }
            else if (module.Name == "mscorlib.dll")
            {
                mscorlibScope = module;
            }
            else
            {
                throw new InvalidOperationException($"Could not resolve System.Object type within '{module.FileName}'.");
            }
            
            var typeRef = new TypeReference(type.Namespace, type.Name, module, mscorlibScope, type.IsValueType);
            return module.ImportReference(typeRef);
        }
    }
}

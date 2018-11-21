// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

namespace Microsoft.AspNetCore.Blazor.BuildTools.Core.ILWipe
{
    class AssemblyItem
    {
        public static IEnumerable<AssemblyItem> ListContents(string assemblyPath)
        {
            var moduleDefinition = ModuleDefinition.ReadModule(assemblyPath);
            return ListContents(moduleDefinition);
        }

        public static IEnumerable<AssemblyItem> ListContents(ModuleDefinition moduleDefinition)
        {
            return moduleDefinition.Types
                .SelectMany(GetNestedTypesRecursive)
                .SelectMany(type => type.Methods)
                .Select(method => new AssemblyItem(method))
                .OrderBy(item => item.ToString(), StringComparer.Ordinal);
        }

        public MethodDefinition Method { get; }

        public AssemblyItem(MethodDefinition method)
        {
            Method = method ?? throw new ArgumentNullException(nameof(method));
        }

        public void WipeFromAssembly(MethodDefinition createMethodWipedException)
        {
            if (!Method.HasBody)
            {
                return; // Nothing to do
            }

            if (Method.HasCustomAttributes)
            {
                for (int i = 0; i < Method.CustomAttributes.Count; i++)
                {
                    if (Method.CustomAttributes[i].AttributeType.FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute")
                    {
                        Method.CustomAttributes.RemoveAt(i);
                        break;
                    }
                }
            }

            // We don't want to actually remove the method definition from the assembly, because
            // then you'd have an assembly that was invalid (it could contain calls to the method
            // that no longer exists). Instead, remove all the instructions from its body, and
            // replace it with "throw CreateMethodWipedException()". Then:
            // [1] The method body is very short, while still definitely being valid (still OK for
            //     it to have any return type)
            // [2] We've removed its references to other methods/types, so they are more likely
            //     to be actually removed fully by a subsequent IL linker pass
            // [3] If the method is actually invoked at runtime, the stack trace will make clear
            //     which method is being excessively wiped
            var il = Method.Body.GetILProcessor();
            il.Body.Instructions.Clear();
            il.Body.Variables.Clear();
            il.Body.ExceptionHandlers.Clear();
            il.Append(il.Create(OpCodes.Call, createMethodWipedException));
            il.Append(il.Create(OpCodes.Throw));
        }

        public override string ToString()
        {
            var result = Method.ToString();
            return result.Substring(result.IndexOf(' ') + 1);
        }

        public int CodeSize
            => Method.Body?.CodeSize ?? 0;

        private static IEnumerable<TypeDefinition> GetNestedTypesRecursive(TypeDefinition type)
        {
            yield return type;

            foreach (var descendant in type.NestedTypes.SelectMany(GetNestedTypesRecursive))
            {
                yield return descendant;
            }
        }
    }
}

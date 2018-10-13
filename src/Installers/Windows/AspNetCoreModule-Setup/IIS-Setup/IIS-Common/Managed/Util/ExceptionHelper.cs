// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Microsoft.WebMatrix.Utility
{
    internal class ExceptionHelper
    {
        private const int PublicKeyTokenLength = 8;

        /// <summary>
        /// This will return a list of assemblies that are present in the call stack for 
        /// the input exception. The list CAN have duplicates.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public static IEnumerable<Assembly> GetAssembliesInCallStack(Exception exception)
        {
            // an AggregateException might have multiple inner exceptions, so we handle it specially
            AggregateException aggregateException = exception as AggregateException;
            if (exception == null)
            {
                return Enumerable.Empty<Assembly>();
            }
            else if (aggregateException == null)
            {
                return GetAssembliesInSingleException(exception).Concat(GetAssembliesInCallStack(exception.InnerException));
            }
            else
            {
                return aggregateException.Flatten().InnerExceptions.SelectMany(ex => GetAssembliesInCallStack(ex));
            }
        }

        private static IEnumerable<Assembly> GetAssembliesInSingleException(Exception exception)
        {
            // some exceptions (like AggregateException) don't have an associated stacktrace
            if (exception != null && exception.StackTrace != null)
            {
                StackTrace stackTrace = new StackTrace(exception, false);
                foreach (StackFrame frame in stackTrace.GetFrames())
                {
                    // DeclaringType can be null for lambdas created by Reflection.Emit
                    Type declaringType = frame.GetMethod().DeclaringType;
                    if (declaringType != null)
                    {
                        Assembly currentAssembly = declaringType.Assembly;
                        Debug.Assert(currentAssembly != null, "currentAssembly must not be null");
                        if (currentAssembly != null)
                        {
                            yield return currentAssembly;
                        }
                    }
                }
            }
        }

        public static IEnumerable<Assembly> RemoveAssembliesThatAreIntheGAC(IEnumerable<Assembly> input)
        {
            foreach (Assembly assembly in input)
            {
                if (!assembly.GlobalAssemblyCache)
                {
                    yield return assembly;
                }
            }
        }

        public static IEnumerable<Assembly> RemoveAssembliesThatAreSignedWithToken(IEnumerable<Assembly> input, byte[] publicKeyToken)
        {
            Debug.Assert(publicKeyToken.Length == PublicKeyTokenLength, "public key tokens should be 8 bytes");
            foreach (Assembly assembly in input)
            {
                byte[] currentToken = assembly.GetName().GetPublicKeyToken();
                bool shouldReturn;
                if (currentToken.Length == 0)
                {
                    // unsigned assembly
                    shouldReturn = true;
                }
                else if (AreTokensTheSame(currentToken, publicKeyToken))
                {
                    // tokens are the same skip the assembly
                    shouldReturn = false;
                }
                else
                {
                    // didnt match anything, return it
                    shouldReturn = true;
                }

                if (shouldReturn)
                {
                    yield return assembly;
                }
            }
        }

        private static bool AreTokensTheSame(byte[] token1, byte[] token2)
        {
            Debug.Assert(
                token1.Length == PublicKeyTokenLength &&
                token2.Length == PublicKeyTokenLength,
                "public key tokens should be 8 bytes");

            for (int i = 0; i < PublicKeyTokenLength; i++)
            {
                if (token1[i] != token2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}

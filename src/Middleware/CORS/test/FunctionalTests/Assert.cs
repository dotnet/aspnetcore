// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Xunit.Sdk;

namespace FunctionalTests
{
    public class Assert : Xunit.Assert
    {
        public static void Success(in ProcessResult processResult)
        {
            if (processResult.ExitCode != 0)
            {
                throw new ProcessAssertException(processResult);
            }
        }

        private class ProcessAssertException : XunitException
        {
            public ProcessAssertException(in ProcessResult processResult)
            {
                Result = processResult;
            }

            public ProcessResult Result { get; }

            public override string Message
            {
                get
                {
                    var message = new StringBuilder();
                    message.Append(Result.ProcessStartInfo.FileName);
                    message.Append(" ");
                    message.Append(Result.ProcessStartInfo.Arguments);
                    message.Append($" exited with {Result.ExitCode}.");
                    message.AppendLine();
                    message.AppendLine();
                    message.Append(Result.Output);
                    return message.ToString();
                }
            }
        }
    }
}

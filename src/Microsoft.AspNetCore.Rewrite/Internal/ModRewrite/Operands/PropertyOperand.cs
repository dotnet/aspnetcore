// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands
{
    public class PropertyOperand : Operand
    {
        public PropertyOperationType Operation { get; }

        public PropertyOperand(PropertyOperationType operation)
        {
            Operation = operation;
        }
        public override bool? CheckOperation(Match previous, string testString, IFileProvider fileProvider)
        {
            switch(Operation)
            {
                case PropertyOperationType.Directory:
                    return fileProvider.GetFileInfo(testString).IsDirectory;
                case PropertyOperationType.RegularFile:
                    return fileProvider.GetFileInfo(testString).Exists;
                case PropertyOperationType.Size:
                    var fileInfo = fileProvider.GetFileInfo(testString);
                    return fileInfo.Exists && fileInfo.Length > 0;
                case PropertyOperationType.ExistingUrl:
                    throw new NotSupportedException("No support for internal sub requests.");
                case PropertyOperationType.ExistingFile:
                    throw new NotSupportedException("No support for internal sub requests.");
                case PropertyOperationType.SymbolicLink:
                    throw new NotSupportedException("No support for checking symbolic links.");
                case PropertyOperationType.Executable:
                    throw new NotSupportedException("No support for checking executable permissions.");
                default:
                    return false;
            }
        }
    }


}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public static class ValidationAttributeUtil
    {
        public static string GetRequiredErrorMessage(string field)
        {
            var attr = new RequiredAttribute();
            return attr.FormatErrorMessage(field);
        }
    }
}
// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    public class DefaultInputFormatterSelector : IInputFormatterSelector
    {
        public IInputFormatter SelectFormatter(InputFormatterContext context)
        {
            var formatters = context.ActionContext.InputFormatters;
            foreach (var formatter in formatters)
            {
                if (formatter.CanRead(context))
                {
                    return formatter;
                }
            }
         
            return null;
        }
    }
}

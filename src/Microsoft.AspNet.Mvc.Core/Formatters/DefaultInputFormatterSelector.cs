// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultInputFormatterSelector : IInputFormatterSelector
    {

        public IInputFormatter SelectFormatter(
            IReadOnlyList<IInputFormatter> inputFormatters, 
            InputFormatterContext context)
        {
            foreach (var formatter in inputFormatters)
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

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public interface IInputFormatterSelector
    {
        IInputFormatter SelectFormatter(
            [NotNull] IReadOnlyList<IInputFormatter> inputFormatters,
            [NotNull] InputFormatterContext context);
    }
}

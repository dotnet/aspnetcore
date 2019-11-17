// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml
{
    public partial class ProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable, System.Xml.Serialization.IXmlSerializable
    {
        internal Microsoft.AspNetCore.Mvc.ProblemDetails ProblemDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class ValidationProblemDetailsWrapper : Microsoft.AspNetCore.Mvc.Formatters.Xml.ProblemDetailsWrapper, Microsoft.AspNetCore.Mvc.Formatters.Xml.IUnwrappable
    {
        internal new Microsoft.AspNetCore.Mvc.ValidationProblemDetails ProblemDetails { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}

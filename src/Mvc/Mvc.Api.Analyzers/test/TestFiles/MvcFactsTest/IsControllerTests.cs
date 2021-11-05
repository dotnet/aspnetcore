// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public interface ITestController { }

    public abstract class AbstractController : Controller { }

    public class DerivedAbstractController : AbstractController { }

    public struct ValueTypeController { }

    public class OpenGenericController<T> : Controller { }

    public class PocoType { }

    public class DerivedPocoType : PocoType { }

    public class TypeDerivingFromController : Controller { }

    public class TypeDerivingFromControllerBase : ControllerBase { }

    public abstract class NoControllerAttributeBaseController { }

    public class NoSuffixNoControllerAttribute : NoControllerAttributeBaseController { }

    public class DerivedGenericController : OpenGenericController<string> { }

    public class NoSuffix : Controller { }

    public class PocoController { }

    [Controller]
    public class CustomBase { }

    [Controller]
    public class ChildOfCustomBase : CustomBase { }

    [NonController]
    public class BaseNonController { }

    [Controller]
    public class ControllerAttributeDerivingFromNonController : BaseNonController { }

    public class BasePocoNonControllerChildController : BaseNonController { }
}

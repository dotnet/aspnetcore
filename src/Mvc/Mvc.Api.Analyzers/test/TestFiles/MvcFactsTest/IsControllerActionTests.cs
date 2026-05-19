// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers
{
    public abstract class TestIsControllerActionBase : ControllerBase
    {
        public abstract IActionResult AbstractMethod();

        public virtual IActionResult VirtualMethod() => null;

        public virtual IActionResult MethodInBase() => null;

        [NonAction]
        public virtual IActionResult NonActionBase() => null;
    }

    public class TestIsControllerAction : TestIsControllerActionBase, IDisposable
    {
        static TestIsControllerAction() { }

        public override IActionResult AbstractMethod() => null;

        private IActionResult PrivateMethod() => null;

        protected IActionResult ProtectedMethod() => null;

        internal IActionResult InternalMethod() => null;

        public IActionResult GenericMethod<T>() => null;

        public static IActionResult StaticMethod() => null;

        [NonAction]
        public IActionResult NonAction() => null;

        public override IActionResult NonActionBase() => null;

        public IActionResult Ordinary() => null;

        public void Dispose() { }
    }

    public class OverridesObjectMethods : ControllerBase
    {
        public override bool Equals(object obj) => false;

        public override int GetHashCode() => 0;

        public new string ToString() => null;
    }

    public class ExplicitIDisposable : ControllerBase, IDisposable
    {
        void IDisposable.Dispose() { }
    }

    public class NotDisposable
    {
        public IActionResult Dispose() => null;
    }

    public class NotDisposableWithExplicitImplementation : IDisposable
    {
        public IActionResult Dispose() => null;

        void IDisposable.Dispose() { }
    }

    public class NotDisposableWithDisposeThatIsNotInterfaceContract : IDisposable
    {
        public IActionResult Dispose(int id) => null;

        void IDisposable.Dispose() { }
    }
}

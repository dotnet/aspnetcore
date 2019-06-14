# Microsoft.AspNetCore.Mvc.Controllers

``` diff
 namespace Microsoft.AspNetCore.Mvc.Controllers {
-    public class ControllerFactoryProvider : IControllerFactoryProvider {
 {
-        public ControllerFactoryProvider(IControllerActivatorProvider activatorProvider, IControllerFactory controllerFactory, IEnumerable<IControllerPropertyActivator> propertyActivators);

-        public Func<ControllerContext, object> CreateControllerFactory(ControllerActionDescriptor descriptor);

-        public Action<ControllerContext, object> CreateControllerReleaser(ControllerActionDescriptor descriptor);

-    }
-    public class DefaultControllerActivator : IControllerActivator {
 {
-        public DefaultControllerActivator(ITypeActivatorCache typeActivatorCache);

-        public virtual object Create(ControllerContext controllerContext);

-        public virtual void Release(ControllerContext context, object controller);

-    }
-    public class DefaultControllerFactory : IControllerFactory {
 {
-        public DefaultControllerFactory(IControllerActivator controllerActivator, IEnumerable<IControllerPropertyActivator> propertyActivators);

-        protected IControllerActivator ControllerActivator { get; }

-        public virtual object CreateController(ControllerContext context);

-        public virtual void ReleaseController(ControllerContext context, object controller);

-    }
 }
```


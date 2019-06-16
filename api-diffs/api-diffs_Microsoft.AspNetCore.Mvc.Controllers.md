# Microsoft.AspNetCore.Mvc.Controllers

``` diff
 namespace Microsoft.AspNetCore.Mvc.Controllers {
     public class ControllerActionDescriptor : ActionDescriptor {
         public ControllerActionDescriptor();
         public virtual string ActionName { get; set; }
         public string ControllerName { get; set; }
         public TypeInfo ControllerTypeInfo { get; set; }
         public override string DisplayName { get; set; }
         public MethodInfo MethodInfo { get; set; }
     }
     public class ControllerActivatorProvider : IControllerActivatorProvider {
         public ControllerActivatorProvider(IControllerActivator controllerActivator);
         public Func<ControllerContext, object> CreateActivator(ControllerActionDescriptor descriptor);
         public Action<ControllerContext, object> CreateReleaser(ControllerActionDescriptor descriptor);
     }
     public class ControllerBoundPropertyDescriptor : ParameterDescriptor, IPropertyInfoParameterDescriptor {
         public ControllerBoundPropertyDescriptor();
         public PropertyInfo PropertyInfo { get; set; }
     }
-    public class ControllerFactoryProvider : IControllerFactoryProvider {
 {
-        public ControllerFactoryProvider(IControllerActivatorProvider activatorProvider, IControllerFactory controllerFactory, IEnumerable<IControllerPropertyActivator> propertyActivators);

-        public Func<ControllerContext, object> CreateControllerFactory(ControllerActionDescriptor descriptor);

-        public Action<ControllerContext, object> CreateControllerReleaser(ControllerActionDescriptor descriptor);

-    }
     public class ControllerFeature {
         public ControllerFeature();
         public IList<TypeInfo> Controllers { get; }
     }
     public class ControllerFeatureProvider : IApplicationFeatureProvider, IApplicationFeatureProvider<ControllerFeature> {
         public ControllerFeatureProvider();
         protected virtual bool IsController(TypeInfo typeInfo);
         public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature);
     }
     public class ControllerParameterDescriptor : ParameterDescriptor, IParameterInfoParameterDescriptor {
         public ControllerParameterDescriptor();
         public ParameterInfo ParameterInfo { get; set; }
     }
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
     public interface IControllerActivator {
         object Create(ControllerContext context);
         void Release(ControllerContext context, object controller);
     }
     public interface IControllerActivatorProvider {
         Func<ControllerContext, object> CreateActivator(ControllerActionDescriptor descriptor);
         Action<ControllerContext, object> CreateReleaser(ControllerActionDescriptor descriptor);
     }
     public interface IControllerFactory {
         object CreateController(ControllerContext context);
         void ReleaseController(ControllerContext context, object controller);
     }
     public interface IControllerFactoryProvider {
         Func<ControllerContext, object> CreateControllerFactory(ControllerActionDescriptor descriptor);
         Action<ControllerContext, object> CreateControllerReleaser(ControllerActionDescriptor descriptor);
     }
     public class ServiceBasedControllerActivator : IControllerActivator {
         public ServiceBasedControllerActivator();
         public object Create(ControllerContext actionContext);
         public virtual void Release(ControllerContext context, object controller);
     }
 }
```


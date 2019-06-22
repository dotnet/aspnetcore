# Microsoft.EntityFrameworkCore.SqlServer.Design.Internal

``` diff
-namespace Microsoft.EntityFrameworkCore.SqlServer.Design.Internal {
 {
-    public class SqlServerAnnotationCodeGenerator : AnnotationCodeGenerator {
 {
-        public SqlServerAnnotationCodeGenerator(AnnotationCodeGeneratorDependencies dependencies);

-        public override MethodCallCodeFragment GenerateFluentApi(IIndex index, IAnnotation annotation);

-        public override MethodCallCodeFragment GenerateFluentApi(IKey key, IAnnotation annotation);

-        public override bool IsHandledByConvention(IModel model, IAnnotation annotation);

-    }
-    public class SqlServerDesignTimeServices : IDesignTimeServices {
 {
-        public SqlServerDesignTimeServices();

-        public virtual void ConfigureDesignTimeServices(IServiceCollection serviceCollection);

-    }
-}
```


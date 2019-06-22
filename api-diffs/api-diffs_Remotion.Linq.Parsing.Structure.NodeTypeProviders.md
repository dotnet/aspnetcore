# Remotion.Linq.Parsing.Structure.NodeTypeProviders

``` diff
-namespace Remotion.Linq.Parsing.Structure.NodeTypeProviders {
 {
-    public sealed class CompoundNodeTypeProvider : INodeTypeProvider {
 {
-        public CompoundNodeTypeProvider(IEnumerable<INodeTypeProvider> innerProviders);

-        public IList<INodeTypeProvider> InnerProviders { get; }

-        public Type GetNodeType(MethodInfo method);

-        public bool IsRegistered(MethodInfo method);

-    }
-    public sealed class MethodInfoBasedNodeTypeRegistry : INodeTypeProvider {
 {
-        public MethodInfoBasedNodeTypeRegistry();

-        public int RegisteredMethodInfoCount { get; }

-        public static MethodInfoBasedNodeTypeRegistry CreateFromRelinqAssembly();

-        public Type GetNodeType(MethodInfo method);

-        public static MethodInfo GetRegisterableMethodDefinition(MethodInfo method, bool throwOnAmbiguousMatch);

-        public bool IsRegistered(MethodInfo method);

-        public void Register(IEnumerable<MethodInfo> methods, Type nodeType);

-    }
-    public sealed class MethodNameBasedNodeTypeRegistry : INodeTypeProvider {
 {
-        public MethodNameBasedNodeTypeRegistry();

-        public int RegisteredNamesCount { get; }

-        public static MethodNameBasedNodeTypeRegistry CreateFromRelinqAssembly();

-        public Type GetNodeType(MethodInfo method);

-        public bool IsRegistered(MethodInfo method);

-        public void Register(IEnumerable<NameBasedRegistrationInfo> registrationInfo, Type nodeType);

-    }
-    public sealed class NameBasedRegistrationInfo {
 {
-        public NameBasedRegistrationInfo(string name, Func<MethodInfo, bool> filter);

-        public Func<MethodInfo, bool> Filter { get; }

-        public string Name { get; }

-    }
-}
```


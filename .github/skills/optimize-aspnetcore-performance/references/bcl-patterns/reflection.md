# Reflection and startup performance

General BCL performance patterns, reconciled across the .NET releases (newest wins). This is the foundation layer: prefer the BCL API here unless the repo has a shared helper with a specific benefit (see [../repo-helpers.md](../repo-helpers.md)). Items are ordered by leverage, hot-path and low-complexity first. See [../decision-framework.md](../decision-framework.md) for when to apply (and the complexity rubric) and [../measuring.md](../measuring.md) for how to verify in this repo.

## Cache ActivatorUtilities ObjectFactory delegates

For dependency-injection construction of a known implementation type and argument shape, create an ObjectFactory once and reuse it.

- Do: Call Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory(type, argumentTypes) once, cache the ObjectFactory, and invoke it with IServiceProvider and the argument array.
- Instead of: Calling ActivatorUtilities.CreateInstance for the same type shape in a tight loop or writing a custom constructor-reflection activator.
- Why: ActivatorUtilities.CreateFactory benefits from MethodInvoker and later delegate-layer reductions, avoiding constructor scanning and reflection on each service creation.
- Since .NET 10. Supersedes: .NET 8 and .NET 9 ActivatorUtilities implementations that already used ConstructorInvoker but still had extra caching and delegate overhead.
- Hot path: yes | Complexity: low
- APIs: `Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateFactory`, `Microsoft.Extensions.DependencyInjection.ObjectFactory`, `Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance`

## Create typed delegates for known method signatures

If the target signature is known, create and cache a strongly typed delegate once and invoke that delegate thereafter.

- Do: Call MethodInfo.CreateDelegate<TDelegate>() or MethodInfo.CreateDelegate(Type) during setup and store the resulting delegate in a static field or cache.
- Instead of: Calling MethodInfo.Invoke repeatedly for a method whose signature is known to the framework author.
- Why: A cached delegate avoids argument validation, boxing arrays, binder work, and repeated MethodInfo.Invoke overhead.
- Since .NET 6. Supersedes: Manual Reflection.Emit invocation stubs and repeated reflection invoke used for known signatures.
- Hot path: yes | Complexity: low
- APIs: `System.Reflection.MethodInfo.CreateDelegate`

## Enumerate multicast delegates without allocating

When custom event or multicast delegate dispatch needs individual handlers, enumerate the invocation list with the allocation-free enumerable.

- Do: Use foreach over Delegate.EnumerateInvocationList<TDelegate>(delegateInstance).
- Instead of: Calling delegateInstance.GetInvocationList() and iterating the allocated array.
- Why: Delegate.EnumerateInvocationList avoids the Delegate[] allocation from GetInvocationList and can significantly reduce dispatch overhead.
- Since .NET 9. Supersedes: Delegate.GetInvocationList array allocation for custom multicast dispatch.
- Hot path: yes | Complexity: low
- APIs: `System.Delegate.EnumerateInvocationList`, `System.Delegate.GetInvocationList`

## Use ConstructorInvoker for repeated dynamic construction

For repeated calls to the same constructor when the constructor is discovered dynamically, create and cache a ConstructorInvoker.

- Do: Create the invoker with System.Reflection.ConstructorInvoker.Create(constructorInfo) and reuse it from a cache keyed by ConstructorInfo or the activation shape.
- Instead of: Repeated ConstructorInfo.Invoke, repeated constructor enumeration, or per-call activator binder work.
- Why: ConstructorInvoker caches first-use information and reduces the repeated work otherwise paid by ConstructorInfo.Invoke or reflection-heavy activators.
- Since .NET 8. Supersedes: Older ConstructorInfo.Invoke and bespoke Reflection.Emit construction stubs.
- Hot path: yes | Complexity: low
- APIs: `System.Reflection.ConstructorInvoker`, `System.Reflection.ConstructorInvoker.Create`, `System.Reflection.ConstructorInvoker.Invoke`

## Use MethodInvoker for unknown signatures

When a method signature is not known at compile time but the same method is invoked repeatedly, create and cache a MethodInvoker.

- Do: Create the invoker once with System.Reflection.MethodInvoker.Create(methodInfo) and call its Invoke overloads with cached arguments.
- Instead of: Repeated MethodBase.Invoke or custom Reflection.Emit stubs for each framework-discovered method.
- Why: MethodInvoker moves first-use preparation out of every call and can be much faster than MethodBase.Invoke for repeated dynamic invocation.
- Since .NET 8. Supersedes: .NET 7 MethodInfo.Invoke internal DynamicMethod optimization when repeated calls can explicitly cache the invoker.
- Hot path: yes | Complexity: low
- APIs: `System.Reflection.MethodInvoker`, `System.Reflection.MethodInvoker.Create`, `System.Reflection.MethodInvoker.Invoke`

## Use generic type intrinsics for closed generic fast paths

In generic code, branch on typeof(T) properties and definitions when specializing behavior for known generic shapes.

- Do: Use patterns such as typeof(T).IsPrimitive or typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Task<>).
- Instead of: Runtime dictionaries of Type metadata or late-bound reflection checks for every operation when a generic typeof(T) branch expresses the same condition.
- Why: .NET 9 can fold typeof(T).IsPrimitive, IsGenericType, and GetGenericTypeDefinition to constants for closed generic instantiations, eliminating branches and reflection calls.
- Since .NET 9. Supersedes: .NET 7 Type.IsByRefLike intrinsic and .NET 8 cached GetGenericTypeDefinition when current generic intrinsics can remove the whole check.
- Hot path: yes | Complexity: low
- APIs: `System.Type.IsPrimitive`, `System.Type.IsGenericType`, `System.Type.GetGenericTypeDefinition`, `System.Type.IsByRefLike`

## Avoid reflection on hot paths entirely

Constrain reflection to startup, build time, or one-time cache population, and keep per-request or per-item paths as typed calls, delegates, generated code, or invokers.

- Do: Discover members once, validate once, store typed delegates, MethodInvoker, ConstructorInvoker, ObjectFactory, UnsafeAccessor calls, or generated metadata in immutable caches.
- Instead of: Calling GetMethod, GetProperty, GetCustomAttributes, MakeGenericType, MethodInfo.Invoke, or FieldInfo.GetValue for each request or item.
- Why: Even with runtime improvements, reflection still pays lookup, validation, boxing, trimming, and metadata costs that compound in framework hot paths.
- Since .NET 6. Supersedes: Older designs that relied on cheaper modern reflection automatically making repeated runtime reflection acceptable.
- Hot path: yes | Complexity: medium
- APIs: `System.Reflection.MethodInfo.CreateDelegate`, `System.Reflection.MethodInvoker`, `System.Reflection.ConstructorInvoker`, `Microsoft.Extensions.DependencyInjection.ObjectFactory`, `System.Runtime.CompilerServices.UnsafeAccessorAttribute`

## Use UnsafeAccessor for required private access

When private or cross-library access is unavoidable, expose a matching extern accessor with UnsafeAccessor instead of using FieldInfo, MethodInfo, or PropertyInfo access on the hot path.

- Do: Declare a static extern method annotated with System.Runtime.CompilerServices.UnsafeAccessorAttribute and, in .NET 10 for hidden parameter or static target types, System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute.
- Instead of: BindingFlags.NonPublic lookups followed by FieldInfo.GetValue, FieldInfo.SetValue, MethodInfo.Invoke, or cross-library private reflection.
- Why: UnsafeAccessor is fixed up to direct member access and is several times faster than reflection field access while avoiding per-call reflection overhead.
- Since .NET 10. Supersedes: .NET 8 UnsafeAccessor for non-generic visible signatures and .NET 9 generic UnsafeAccessor when the target type could be expressed directly; replaces private reflection patterns.
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.CompilerServices.UnsafeAccessorAttribute`, `System.Runtime.CompilerServices.UnsafeAccessorTypeAttribute`, `System.Runtime.CompilerServices.UnsafeAccessorKind`

## Use generic UnsafeAccessor support for generic private members

For generic private fields or methods, put the UnsafeAccessor on a generic accessor type or method so the runtime can bind the closed generic member directly.

- Do: Define Accessors<T> with an extern method annotated [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_field")] returning the precise generic type or ref.
- Instead of: typeof(MyType<T>).GetField(...).GetValue(instance) and casting on every access.
- Why: Generic UnsafeAccessor keeps private generic access near direct-call cost and avoids typed casts and FieldInfo.GetValue overhead.
- Since .NET 9. Supersedes: .NET 8 UnsafeAccessor limitation to non-generic members and older private reflection access.
- Hot path: yes | Complexity: medium
- APIs: `System.Runtime.CompilerServices.UnsafeAccessorAttribute`, `System.Runtime.CompilerServices.UnsafeAccessorKind.Field`

## Cache generic type metadata and constructed types

Resolve generic type definitions, generic arguments, and constructed generic types once per shape and reuse them.

- Do: Cache Type.GetGenericTypeDefinition(), Type.MakeGenericType(...), and common Type[] argument arrays such as typeof(T) shape keys.
- Instead of: Repeatedly constructing Type[] arrays and calling MakeGenericType or GetGenericTypeDefinition in request paths.
- Why: Modern runtimes cache and special-case some generic reflection operations, but repeated MakeGenericType and metadata traversal still add avoidable startup and hot-path cost.
- Since .NET 8. Supersedes: .NET 6 one-parameter MakeGenericType special-casing as the sole optimization; .NET 9 intrinsics may supersede checks expressible as typeof(T) constants.
- Hot path: either | Complexity: low
- APIs: `System.Type.GetGenericTypeDefinition`, `System.Type.MakeGenericType`, `System.Type.GetGenericArguments`

## Guide DI constructor selection explicitly

When a type has multiple constructors and is dynamically activated by DI, mark the intended constructor so activators do less discovery and make stable choices.

- Do: Annotate the chosen constructor with Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute and cache factories where possible.
- Instead of: Leaving a large overload set for every ActivatorUtilities.CreateInstance call to inspect and resolve.
- Why: Explicit constructor selection reduces ambiguity, avoids unnecessary constructor examination, and composes with ActivatorUtilities improvements.
- Since .NET 9. Supersedes: Repeated constructor scanning in older ActivatorUtilities activation paths.
- Hot path: either | Complexity: low
- APIs: `Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructorAttribute`, `Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance`

## Prefer metadata-only attribute checks when possible

When code only needs to know whether an attribute exists or inspect its metadata, avoid constructing attribute instances.

- Do: Use Attribute.IsDefined, MemberInfo.IsDefined, or MemberInfo.GetCustomAttributesData and cache the result per member.
- Instead of: Calling GetCustomAttributes and constructing all attribute instances just to test presence or read simple metadata.
- Why: Attribute materialization can allocate and run attribute constructors or property setters, whereas IsDefined and CustomAttributeData are cheaper and have received allocation reductions.
- Since .NET 6. Supersedes: Older broad GetCustomAttributes scans used for simple existence checks.
- Hot path: either | Complexity: low
- APIs: `System.Attribute.IsDefined`, `System.Reflection.MemberInfo.IsDefined`, `System.Reflection.MemberInfo.GetCustomAttributesData`, `System.Attribute.GetCustomAttributes`

## Prefer source generators over runtime reflection

Move metadata discovery, binding, serialization, logging, and pattern generation from runtime reflection to build-time source generation whenever an available generator fits.

- Do: Use source-generated entry points such as System.Text.Json source generation, LoggerMessage source generation, GeneratedRegexAttribute, or a custom incremental generator that emits typed binders/accessors.
- Instead of: Runtime reflection scans, late-bound member lookup, Reflection.Emit stubs, or repeated MethodInfo.Invoke during startup and per request.
- Why: Generated code removes startup scanning, avoids reflection-only metadata dependencies, improves trimming and Native AOT compatibility, and keeps hot paths as normal typed calls.
- Since .NET 7. Supersedes: Runtime reflection and Reflection.Emit based startup discovery patterns used before broad source-generator support in .NET 6 and .NET 7.
- Hot path: either | Complexity: low
- APIs: `System.Text.Json.Serialization.JsonSerializableAttribute`, `Microsoft.Extensions.Logging.LoggerMessageAttribute`, `System.Text.RegularExpressions.GeneratedRegexAttribute`

## Reuse FieldInfo when reflection field access is unavoidable

If field reflection cannot be replaced, resolve the FieldInfo once and reuse the same instance so the runtime cached field accessor can pay off.

- Do: Store FieldInfo objects in static or per-type caches and prefer SetValue/GetValue only outside the hottest paths.
- Instead of: Repeated typeof(T).GetField followed by one-off FieldInfo.GetValue or FieldInfo.SetValue calls.
- Why: .NET 9 caches a field accessor on FieldInfo, greatly speeding subsequent GetValue and SetValue for several field shapes.
- Since .NET 9. Supersedes: .NET 8 reflection field access without the FieldInfo-cached accessor; for private access in current code, prefer UnsafeAccessor instead.
- Hot path: either | Complexity: low
- APIs: `System.Reflection.FieldInfo.GetValue`, `System.Reflection.FieldInfo.SetValue`, `System.Type.GetField`

## Use Activator.CreateInstance for parameterless dynamic construction only after considering factories

For parameterless dynamic construction, prefer cached compiled factories when hot, but use modern Activator.CreateInstance for simple cold paths rather than custom reflection code.

- Do: Use new() constraints, Activator.CreateInstance<T>(), or cached factory delegates depending on how dynamic and hot the construction path is.
- Instead of: Manually finding the default constructor and invoking it repeatedly through ConstructorInfo.Invoke.
- Why: .NET 6 added per-type function-pointer caching for Activator.CreateInstance, making it much cheaper while still not as optimal as cached typed factories for hot paths.
- Since .NET 6. Supersedes: Older manual constructor reflection used to avoid slower Activator.CreateInstance implementations.
- Hot path: either | Complexity: low
- APIs: `System.Activator.CreateInstance`, `System.Activator.CreateInstance<T>`

## Use Type.GetType and TypeName parsing sparingly and cache results

Treat assembly-qualified type-name parsing and rendering as startup work and cache the resulting Type or parsed TypeName.

- Do: Parse once with System.Type.GetType or System.Reflection.Metadata.TypeName.Parse and store the result keyed by the input name.
- Instead of: Repeatedly parsing the same complex type-name string for each request or serialized record.
- Why: .NET 9 and .NET 10 reduced TypeName parsing allocations, but parsing complex nested generic names still costs microseconds and kilobytes.
- Since .NET 10. Supersedes: .NET 9 consolidated Type.GetType parsing improvements; current TypeName.Parse rendering is faster but still should be cached.
- Hot path: either | Complexity: low
- APIs: `System.Type.GetType`, `System.Reflection.Metadata.TypeName.Parse`, `System.Reflection.Metadata.TypeName.FullName`

## Use zero-length singleton arrays for reflection calls

Pass existing zero-length singletons for no-argument reflection and activation paths, and avoid allocating empty Type[] or object[] values.

- Do: Use Type.EmptyTypes, Array.Empty<object>(), and cached argument arrays where APIs require arrays.
- Instead of: new Type[0], new object[0], or constructing a fresh empty array for every Invoke, CreateInstance, or factory creation.
- Why: Empty array allocations show up in startup reflection caches and dynamic activation, and avoiding them lowers allocation pressure.
- Since .NET 7. Supersedes: Older activator and reflection helper code that allocated empty arrays during CreateInstance and cache population.
- Hot path: either | Complexity: low
- APIs: `System.Type.EmptyTypes`, `System.Array.Empty`

## Let modern Invoke handle rare dynamic calls

For rare dynamic calls where caching an invoker is not worth the complexity, use MethodInfo.Invoke directly on modern .NET instead of emitting custom stubs.

- Do: Use MethodInfo.Invoke for cold or one-off calls, and graduate to CreateDelegate or MethodInvoker only when measurements show repeated invocation cost.
- Instead of: Maintaining custom DynamicMethod or Reflection.Emit invoke stubs for cold-path reflection.
- Why: .NET 7 and .NET 8 made MethodBase.Invoke much faster internally, reducing the need for fragile Reflection.Emit in non-hot paths.
- Since .NET 8. Supersedes: .NET 7 internal DynamicMethod invoke optimization for many cases; source generation still supersedes reflection for startup-heavy patterns.
- Hot path: cold | Complexity: low
- APIs: `System.Reflection.MethodBase.Invoke`, `System.Reflection.MethodInfo.Invoke`

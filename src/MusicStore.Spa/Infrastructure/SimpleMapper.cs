using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MusicStore.Spa.Infrastructure
{
    public class SimpleMapper
    {
        private static readonly Expression _emptyExp = Expression.Empty();
        private static ConcurrentDictionary<Tuple<Type, Type>, Delegate> _mapCache = new ConcurrentDictionary<Tuple<Type, Type>, Delegate>();

        public static TDest Map<TSource, TDest>(TSource source, TDest dest)
        {
            var map = (Func<TSource, TDest, TDest>)_mapCache.GetOrAdd(Tuple.Create(typeof(TSource), typeof(TDest)), _ => MakeMapMethod<TSource, TDest>());
            return map(source, dest);
        }

        private static Func<TSource, TDest, TDest> MakeMapMethod<TSource, TDest>()
        {
            // TODO: Support convention-based mapping, e.g. AlbumTitle <- Album.Title
            // TODO: Support mapping to/from fields

            var sourceProps = typeof(TSource).GetRuntimeProperties().ToDictionary(p => p.Name);
            var destProps = typeof(TDest).GetRuntimeProperties().ToDictionary(p => p.Name);
            
            var destArg = Expression.Parameter(typeof(TDest), "dest");
            var srcArg = Expression.Parameter(typeof(TSource), "src");

            var assignments = MakeAssignments(typeof(TSource), typeof(TDest), srcArg, destArg);

            if (!assignments.Any())
            {
                throw new InvalidOperationException(string.Format("No matching properties were found between the types {0} and {1}", typeof(TSource), typeof(TDest)));
            }

            var assignmentsBlock = Expression.Block(assignments);
            var blockExp = Expression.Block(typeof(TDest), assignmentsBlock, destArg);
            var map = Expression.Lambda<Func<TSource, TDest, TDest>>(blockExp, srcArg, destArg);

            return map.Compile();
        }

        private static IEnumerable<Expression> MakeAssignments(Type sourceType, Type destType, Expression sourcePropertyExp, Expression destPropertyExp)
        {
            var sourceProps = sourceType.GetRuntimeProperties().ToDictionary(p => p.Name);
            var destProps = destType.GetRuntimeProperties().ToDictionary(p => p.Name);
            var assignments = new List<Expression>();

            foreach (var srcProp in sourceProps)
            {
                if (!srcProp.Value.GetMethod.IsPublic) continue;

                var destProp = destProps.ContainsKey(srcProp.Key) ? destProps[srcProp.Key] : null;
                if (destProp != null && destProp.SetMethod != null)
                {
                    var destPropType = destProp.PropertyType;
                    var srcPropType = srcProp.Value.PropertyType;

                    var srcPropExp = Expression.Property(sourcePropertyExp, srcProp.Value);
                    var destPropExp = Expression.Property(destPropertyExp, destProp);

                    if (destPropType.GetTypeInfo().IsAssignableFrom(srcPropType.GetTypeInfo()) && destProp.SetMethod.IsPublic)
                    {
                        var assignmentExp = Expression.Assign(destPropExp, srcPropExp);
                        // dest.Prop = src.Prop;
                        assignments.Add(assignmentExp);
                    }
                    else if (destProp.GetMethod.IsPublic)
                    {
                        // The properties aren't assignable but they may have members that are
                        var deepAssignmentExp = MakeAssignments(srcPropType, destPropType, srcPropExp, destPropExp);
                        if (deepAssignmentExp.Any())
                        {
                            // Check if dest is null and if so skip for now
                            // - if (dest.Foo != null && src.Foo != null) {
                            // -     dest.Foo.Bar = src.Foo.Bar;
                            // - }
                            var nullCheckExp = Expression.And(Expression.NotEqual(destPropExp, Expression.Constant(null)),
                                                              Expression.NotEqual(srcPropExp, Expression.Constant(null)));
                            var nullCheckExpBlock = Expression.IfThen(nullCheckExp, Expression.Block(deepAssignmentExp));
                            assignments.Add(nullCheckExpBlock);
                        }   
                    }
                }
            }

            return assignments;
        }
    }
}
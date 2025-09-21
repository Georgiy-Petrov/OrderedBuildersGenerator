using System;
using System.Collections.Generic;
using System.Linq;
using OrderedBuildersGenerator.EquatableCollections;

namespace OrderedBuildersGenerator;

internal static class Extensions
{
    internal static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T> collection) where T : IEquatable<T>?
    {
        return new EquatableArray<T>(collection.ToArray());
    }

    internal static TResult? TryCast<TResult>(this object @object) where TResult : class
    {
        return @object as TResult;
    }
}
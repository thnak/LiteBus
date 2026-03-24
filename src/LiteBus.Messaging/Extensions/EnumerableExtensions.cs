using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Messaging.Extensions;

public static class EnumerableExtensions
{
    public static ILazyHandlerCollection<THandler, TDescriptor> ToLazyReadOnlyCollection<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] THandler, TDescriptor>(
        this IEnumerable<LazyHandler<THandler, TDescriptor>> source)
        where TDescriptor : IHandlerDescriptor
    {
        return new LazyHandlerCollection<THandler, TDescriptor>(source);
    }
}
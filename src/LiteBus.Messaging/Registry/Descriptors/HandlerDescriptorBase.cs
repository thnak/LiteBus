using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Messaging.Registry.Descriptors;

internal abstract class HandlerDescriptorBase : IHandlerDescriptor
{
    public required Type MessageType { get; init; }

    public required int Priority { get; init; }

    public required IReadOnlyCollection<string> Tags { get; init; }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public required Type HandlerType { get; init; }
}
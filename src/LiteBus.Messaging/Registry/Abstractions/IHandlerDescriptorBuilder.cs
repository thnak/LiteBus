using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Messaging.Registry.Abstractions;

internal interface IHandlerDescriptorBuilder
{
    bool CanBuild([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type);

    IEnumerable<IHandlerDescriptor> Build([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type);
}
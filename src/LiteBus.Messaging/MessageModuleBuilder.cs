using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Messaging;

public sealed class MessageModuleBuilder
{
    private readonly IMessageRegistry _messageRegistry;

    public MessageModuleBuilder(IMessageRegistry messageRegistry)
    {
        _messageRegistry = messageRegistry;
    }

    public MessageModuleBuilder Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        _messageRegistry.Register(typeof(T));

        return this;
    }

    public MessageModuleBuilder Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        _messageRegistry.Register(type);
        return this;
    }

    /// <summary>
    ///     Registers multiple types for the message registry.
    ///     This overload is designed to be called with a compile-time-generated collection (e.g.
    ///     <c>GeneratedLiteBusHandlers.All</c>) for AOT-safe registration without reflection.
    /// </summary>
    /// <param name="types">The types to register.</param>
    /// <returns>The current <see cref="MessageModuleBuilder" /> instance for method chaining.</returns>
    public MessageModuleBuilder Register(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            _messageRegistry.Register(type);
        }

        return this;
    }

    [RequiresUnreferencedCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with trimming. Use Register<T>() for each type instead.")]
    [RequiresDynamicCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with Native AOT. Use Register<T>() for each type instead.")]
    public MessageModuleBuilder RegisterFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            _messageRegistry.Register(type);
        }

        return this;
    }
}
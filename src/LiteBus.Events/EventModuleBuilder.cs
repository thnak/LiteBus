using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Events;

/// <summary>
///     Builder class for registering event types in the message registry.
/// </summary>
public sealed class EventModuleBuilder
{
    private readonly IMessageRegistry _messageRegistry;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventModuleBuilder" /> class.
    /// </summary>
    /// <param name="messageRegistry">The message registry to which events will be registered.</param>
    public EventModuleBuilder(IMessageRegistry messageRegistry)
    {
        _messageRegistry = messageRegistry;
    }

    /// <summary>
    ///     Registers an event type for the message registry.
    /// </summary>
    /// <typeparam name="T">The type of event to register, which must implement <see cref="IRegistrableEventConstruct" />.</typeparam>
    /// <returns>The current <see cref="EventModuleBuilder" /> instance for method chaining.</returns>
    public EventModuleBuilder Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : IRegistrableEventConstruct
    {
        _messageRegistry.Register(typeof(T));
        return this;
    }

    /// <summary>
    ///     Registers an event type for the message registry.
    /// </summary>
    /// <param name="type">The type of event to register, which must implement <see cref="IRegistrableEventConstruct" />.</param>
    /// <returns>The current <see cref="EventModuleBuilder" /> instance for method chaining.</returns>
    public EventModuleBuilder Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (!type.IsAssignableTo(typeof(IRegistrableEventConstruct)))
        {
            throw new NotSupportedException($"The given type '{type.Name}' is not an event construct and cannot be registered.");
        }

        _messageRegistry.Register(type);
        return this;
    }

    /// <summary>
    ///     Registers multiple event types for the message registry.
    ///     This overload is designed to be called with a compile-time-generated collection (e.g.
    ///     <c>GeneratedLiteBusHandlers.EventHandlers</c>) for AOT-safe registration without reflection.
    ///     When called with the source-generated collection, every type in the list already implements
    ///     <see cref="IRegistrableEventConstruct" />, so the guard below is only a safety net for
    ///     arbitrary callers that pass types not produced by the generator.
    /// </summary>
    /// <param name="types">The types to register. Each type must implement <see cref="IRegistrableEventConstruct" />.</param>
    /// <returns>The current <see cref="EventModuleBuilder" /> instance for method chaining.</returns>
    public EventModuleBuilder Register(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IRegistrableEventConstruct)))
            {
                throw new NotSupportedException($"The given type '{type.Name}' is not an event construct and cannot be registered.");
            }

            _messageRegistry.Register(type);
        }

        return this;
    }

    /// <summary>
    ///     Registers all event types from the specified assembly that implement <see cref="IRegistrableEventConstruct" />.
    /// </summary>
    /// <param name="assembly">The assembly from which to register event types.</param>
    /// <returns>The current <see cref="EventModuleBuilder" /> instance for method chaining.</returns>
    [RequiresUnreferencedCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with trimming. Use Register<T>() for each type instead.")]
    [RequiresDynamicCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with Native AOT. Use Register<T>() for each type instead.")]
    public EventModuleBuilder RegisterFromAssembly(Assembly assembly)
    {
        foreach (var registrableEventConstruct in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(IRegistrableEventConstruct))))
        {
            _messageRegistry.Register(registrableEventConstruct);
        }

        return this;
    }
}
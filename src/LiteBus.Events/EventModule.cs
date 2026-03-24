using System;
using System.Collections.Generic;
using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Registry;
using LiteBus.Runtime.Abstractions;

namespace LiteBus.Events;

/// <summary>
///     Module for configuring event handling infrastructure.
///     Depends on the messaging module for core messaging functionality.
/// </summary>
public sealed class EventModule : IModule
{
    private readonly Action<EventModuleBuilder> _builder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventModule" /> class.
    /// </summary>
    /// <param name="builder">The configuration action for the event module.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public EventModule(Action<EventModuleBuilder> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <summary>
    ///     Builds the event module by configuring event handlers and registering event-specific services.
    /// </summary>
    /// <param name="configuration">The module configuration containing dependency registry and shared context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is null.</exception>
    public void Build(IModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var messageRegistry = MessageRegistryAccessor.Instance;

        var moduleBuilder = new EventModuleBuilder(messageRegistry);
        _builder(moduleBuilder);

        RegisterEventServices(configuration);
        RegisterNewHandlers(configuration, messageRegistry, moduleBuilder.GetRegisteredTypes());
    }

    /// <summary>
    ///     Registers event-specific services with the dependency registry.
    /// </summary>
    /// <param name="configuration">The module configuration.</param>
    private static void RegisterEventServices(IModuleConfiguration configuration)
    {
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IEventMediator),
            typeof(EventMediator)));

        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IEventPublisher),
            typeof(EventMediator)));
    }

    /// <summary>
    ///     Registers handler types that were discovered during this module's configuration.
    /// </summary>
    /// <param name="configuration">The module configuration.</param>
    /// <param name="messageRegistry">The message registry containing handler information.</param>
    /// <param name="requestedTypes">
    ///     The set of types explicitly requested by this builder call.
    ///     Handlers whose <see cref="IHandlerDescriptor.HandlerType" /> is in this set will be registered
    ///     in the dependency container, regardless of whether they were already present in the global
    ///     registry (e.g. when multiple DI containers are configured concurrently, as in MS Orleans multi-silo tests).
    /// </param>
    private static void RegisterNewHandlers(IModuleConfiguration configuration, IMessageRegistry messageRegistry, HashSet<Type> requestedTypes)
    {
        foreach (var handlerDescriptor in messageRegistry.Handlers)
        {
            var handlerType = handlerDescriptor.HandlerType;

            if (handlerType is { IsClass: true, IsAbstract: false } && requestedTypes.Contains(handlerType))
            {
                configuration.DependencyRegistry.Register(new DependencyDescriptor(
                    handlerType,
                    handlerType));
            }
        }
    }
}
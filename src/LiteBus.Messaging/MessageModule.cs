using System;
using System.Collections.Generic;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Mediator;
using LiteBus.Messaging.Registry;
using LiteBus.Runtime.Abstractions;

namespace LiteBus.Messaging;

/// <summary>
///     Module for configuring messaging infrastructure components.
///     This is a foundational module that other modules depend on.
/// </summary>
public sealed class MessageModule : IModule
{
    private readonly Action<MessageModuleBuilder> _builder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageModule" /> class.
    /// </summary>
    /// <param name="builder">The configuration action for the message module.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when builder is null.</exception>
    public MessageModule(Action<MessageModuleBuilder> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <inheritdoc />
    public void Build(IModuleConfiguration configuration)
    {
        // Create or get the message registry - this will be shared across all messaging-related modules
        var messageRegistry = MessageRegistryAccessor.Instance;

        configuration.SetContext(messageRegistry);

        // Configure the message module using the builder
        var moduleBuilder = new MessageModuleBuilder(messageRegistry);
        _builder(moduleBuilder);

        // Register core messaging services
        RegisterMessagingServices(configuration, messageRegistry);
        RegisterNewHandlers(configuration, messageRegistry, moduleBuilder.GetRegisteredTypes());
    }

    /// <summary>
    ///     Registers core messaging services with the dependency registry.
    /// </summary>
    /// <param name="configuration">The module configuration.</param>
    /// <param name="messageRegistry">The message registry instance.</param>
    private static void RegisterMessagingServices(
        IModuleConfiguration configuration,
        IMessageRegistry messageRegistry)
    {
        // Register message registry as singleton
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IMessageRegistry),
            messageRegistry));

        // Register message mediator as transient
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IMessageMediator),
            typeof(MessageMediator)));

        // Register execution context accessor as transient factory
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IExecutionContext),
            _ => AmbientExecutionContext.Current));
    }

    /// <summary>
    ///     Registers handler types that were added to the message registry during module building.
    /// </summary>
    /// <param name="configuration">The module configuration to register handlers with.</param>
    /// <param name="messageRegistry">The message registry containing the handlers.</param>
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
                configuration.DependencyRegistry.Register(new DependencyDescriptor(handlerType, handlerType));
            }
        }
    }
}
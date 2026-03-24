using System;
using System.Collections.Generic;
using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Registry;
using LiteBus.Runtime.Abstractions;

namespace LiteBus.Commands;

/// <summary>
///     Module for configuring command handling infrastructure.
///     Depends on the messaging module for core messaging functionality.
/// </summary>
public sealed class CommandModule : IModule
{
    private readonly Action<CommandModuleBuilder> _builder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandModule" /> class.
    /// </summary>
    /// <param name="builder">The configuration action for the command module.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public CommandModule(Action<CommandModuleBuilder> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <summary>
    ///     Builds the command module by configuring command handlers and registering command-specific services.
    /// </summary>
    /// <param name="configuration">The module configuration containing dependency registry and shared context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is null.</exception>
    public void Build(IModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var messageRegistry = MessageRegistryAccessor.Instance;

        var moduleBuilder = new CommandModuleBuilder(messageRegistry);
        _builder(moduleBuilder);

        RegisterCommandServices(configuration, moduleBuilder.BuildInboxCommandSet());
        RegisterNewHandlers(configuration, messageRegistry, moduleBuilder.GetRegisteredTypes());
    }

    /// <summary>
    ///     Registers command-specific services with the dependency registry.
    /// </summary>
    /// <param name="configuration">The module configuration.</param>
    /// <param name="inboxCommandSet">The compile-time-built set of inbox command types.</param>
    private static void RegisterCommandServices(IModuleConfiguration configuration, InboxCommandSet inboxCommandSet)
    {
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(ICommandMediator),
            typeof(CommandMediator)));

        // Register the inbox command set as a singleton so CommandMediator can resolve it without reflection.
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(ICommandInboxTypeSet),
            inboxCommandSet));
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
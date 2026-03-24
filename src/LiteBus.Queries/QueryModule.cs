using System;
using System.Collections.Generic;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Registry;
using LiteBus.Queries.Abstractions;
using LiteBus.Runtime.Abstractions;

namespace LiteBus.Queries;

/// <summary>
///     Module for configuring query handling infrastructure.
///     Depends on the messaging module for core messaging functionality.
/// </summary>
public sealed class QueryModule : IModule
{
    private readonly Action<QueryModuleBuilder> _builder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueryModule" /> class.
    /// </summary>
    /// <param name="builder">The configuration action for the query module.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder" /> is null.</exception>
    public QueryModule(Action<QueryModuleBuilder> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <summary>
    ///     Builds the query module by configuring query handlers and registering query-specific services.
    /// </summary>
    /// <param name="configuration">The module configuration containing dependency registry and shared context.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration" /> is null.</exception>
    public void Build(IModuleConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var messageRegistry = MessageRegistryAccessor.Instance;

        var moduleBuilder = new QueryModuleBuilder(messageRegistry);
        _builder(moduleBuilder);

        RegisterQueryServices(configuration);
        RegisterNewHandlers(configuration, messageRegistry, moduleBuilder.GetRegisteredTypes());
    }

    /// <summary>
    ///     Registers query-specific services with the dependency registry.
    /// </summary>
    /// <param name="configuration">The module configuration.</param>
    private static void RegisterQueryServices(IModuleConfiguration configuration)
    {
        configuration.DependencyRegistry.Register(new DependencyDescriptor(
            typeof(IQueryMediator),
            typeof(QueryMediator)));
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
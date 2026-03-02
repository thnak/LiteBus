using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LiteBus.Messaging.Abstractions;
using LiteBus.Queries.Abstractions;

namespace LiteBus.Queries;

/// <summary>
///     Builder class for registering query types in the message registry.
/// </summary>
public sealed class QueryModuleBuilder
{
    private readonly IMessageRegistry _messageRegistry;

    /// <summary>
    ///     Initializes a new instance of the <see cref="QueryModuleBuilder" /> class.
    /// </summary>
    /// <param name="messageRegistry">The message registry to which queries will be registered.</param>
    public QueryModuleBuilder(IMessageRegistry messageRegistry)
    {
        _messageRegistry = messageRegistry;
    }

    /// <summary>
    ///     Registers a query type for the message registry.
    /// </summary>
    /// <typeparam name="T">The type of query to register, which must implement <see cref="IRegistrableQueryConstruct" />.</typeparam>
    /// <returns>The current <see cref="QueryModuleBuilder" /> instance for method chaining.</returns>
    public QueryModuleBuilder Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : IRegistrableQueryConstruct
    {
        _messageRegistry.Register(typeof(T));
        return this;
    }

    /// <summary>
    ///     Registers a query type for the message registry.
    /// </summary>
    /// <param name="type">The type of query to register, which must implement <see cref="IRegistrableQueryConstruct" />.</param>
    /// <returns>The current <see cref="QueryModuleBuilder" /> instance for method chaining.</returns>
    public QueryModuleBuilder Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (!type.IsAssignableTo(typeof(IRegistrableQueryConstruct)))
        {
            throw new NotSupportedException($"The given type '{type.Name}' is not a query construct and cannot be registered.");
        }

        _messageRegistry.Register(type);
        return this;
    }

    /// <summary>
    ///     Registers multiple query types for the message registry.
    ///     This overload is designed to be called with a compile-time-generated collection (e.g.
    ///     <c>GeneratedLiteBusHandlers.QueryHandlers</c>) for AOT-safe registration without reflection.
    ///     When called with the source-generated collection, every type in the list already implements
    ///     <see cref="IRegistrableQueryConstruct" />, so the guard below is only a safety net for
    ///     arbitrary callers that pass types not produced by the generator.
    /// </summary>
    /// <param name="types">The types to register. Each type must implement <see cref="IRegistrableQueryConstruct" />.</param>
    /// <returns>The current <see cref="QueryModuleBuilder" /> instance for method chaining.</returns>
    public QueryModuleBuilder Register(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IRegistrableQueryConstruct)))
            {
                throw new NotSupportedException($"The given type '{type.Name}' is not a query construct and cannot be registered.");
            }

            _messageRegistry.Register(type);
        }

        return this;
    }

    /// <summary>
    ///     Registers all query types from the specified assembly that implement <see cref="IRegistrableQueryConstruct" />.
    /// </summary>
    /// <param name="assembly">The assembly from which to register query types.</param>
    /// <returns>The current <see cref="QueryModuleBuilder" /> instance for method chaining.</returns>
    [RequiresUnreferencedCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with trimming. Use Register<T>() for each type instead.")]
    [RequiresDynamicCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with Native AOT. Use Register<T>() for each type instead.")]
    public QueryModuleBuilder RegisterFromAssembly(Assembly assembly)
    {
        foreach (var registrableQueryConstruct in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(IRegistrableQueryConstruct))))
        {
            _messageRegistry.Register(registrableQueryConstruct);
        }

        return this;
    }
}
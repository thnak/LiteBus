using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Messaging;

public sealed class MessageModuleBuilder
{
    private readonly IMessageRegistry _messageRegistry;
    private readonly HashSet<Type> _registeredTypes = [];

    public MessageModuleBuilder(IMessageRegistry messageRegistry)
    {
        _messageRegistry = messageRegistry;
    }

    public MessageModuleBuilder Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
    {
        _messageRegistry.Register(typeof(T));
        _registeredTypes.Add(typeof(T));

        return this;
    }

    public MessageModuleBuilder Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        _messageRegistry.Register(type);
        _registeredTypes.Add(type);
        return this;
    }

    /// <summary>
    ///     Registers multiple types for the message registry.
    ///     This overload is designed to be called with a compile-time-generated collection (e.g.
    ///     <c>GeneratedLiteBusHandlers.All</c>) for AOT-safe registration without reflection.
    /// </summary>
    /// <param name="types">The types to register.</param>
    /// <returns>The current <see cref="MessageModuleBuilder" /> instance for method chaining.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Types in this collection are expected to come from typeof() expressions (e.g. source-generated collections) whose metadata is preserved by the trimmer.")]
    public MessageModuleBuilder Register(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            _messageRegistry.Register(type);
            _registeredTypes.Add(type);
        }

        return this;
    }

    /// <summary>
    ///     Registers all types from the specified assembly with the message registry.
    /// </summary>
    /// <param name="assembly">The assembly from which to register types.</param>
    /// <returns>The current <see cref="MessageModuleBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     This method uses <see cref="Assembly.GetTypes"/> which is not compatible with trimming or Native AOT.
    ///     Prefer using <see cref="Register(IEnumerable{Type})"/> with a source-generated type list for AOT scenarios.
    /// </remarks>
    [RequiresUnreferencedCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with trimming. Use Register(IEnumerable<Type>) with a source-generated type list instead.")]
    [RequiresDynamicCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with Native AOT. Use Register(IEnumerable<Type>) with a source-generated type list instead.")]
    public MessageModuleBuilder RegisterFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            _messageRegistry.Register(type);
            _registeredTypes.Add(type);
        }

        return this;
    }

    /// <summary>
    ///     Gets the set of types that were requested to be registered by this builder instance.
    ///     Used by <see cref="MessageModule" /> to determine which handler descriptors belong
    ///     to this specific build call, regardless of global registry deduplication.
    /// </summary>
    internal HashSet<Type> GetRegisteredTypes() => _registeredTypes;
}
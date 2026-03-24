using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Commands;

/// <summary>
///     Builder class for registering command types in the message registry.
/// </summary>
public sealed class CommandModuleBuilder
{
    private readonly IMessageRegistry _messageRegistry;
    private readonly List<Type> _inboxCommandTypes = [];
    private readonly HashSet<Type> _registeredTypes = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandModuleBuilder" /> class.
    /// </summary>
    /// <param name="messageRegistry">The message registry to which commands will be registered.</param>
    public CommandModuleBuilder(IMessageRegistry messageRegistry)
    {
        _messageRegistry = messageRegistry;
    }

    /// <summary>
    ///     Registers a command type for the message registry.
    /// </summary>
    /// <typeparam name="T">The type of command to register, which must implement <see cref="IRegistrableCommandConstruct" />.</typeparam>
    /// <returns>The current <see cref="CommandModuleBuilder" /> instance for method chaining.</returns>
    public CommandModuleBuilder Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : IRegistrableCommandConstruct
    {
        _messageRegistry.Register(typeof(T));
        _registeredTypes.Add(typeof(T));
        return this;
    }

    /// <summary>
    ///     Registers a command type for the message registry.
    /// </summary>
    /// <param name="type">The type of command to register, which must implement <see cref="IRegistrableCommandConstruct" />.</param>
    /// <returns>The current <see cref="CommandModuleBuilder" /> instance for method chaining.</returns>
    public CommandModuleBuilder Register([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        if (!type.IsAssignableTo(typeof(IRegistrableCommandConstruct)))
        {
            throw new NotSupportedException($"The given type '{type.Name}' is not a command construct and cannot be registered.");
        }

        _messageRegistry.Register(type);
        _registeredTypes.Add(type);
        return this;
    }

    /// <summary>
    ///     Registers multiple command types for the message registry.
    ///     This overload is designed to be called with a compile-time-generated collection (e.g.
    ///     <c>GeneratedLiteBusHandlers.CommandHandlers</c>) for AOT-safe registration without reflection.
    ///     When called with the source-generated collection, every type in the list already implements
    ///     <see cref="IRegistrableCommandConstruct" />, so the guard below is only a safety net for
    ///     arbitrary callers that pass types not produced by the generator.
    /// </summary>
    /// <param name="types">The types to register. Each type must implement <see cref="IRegistrableCommandConstruct" />.</param>
    /// <returns>The current <see cref="CommandModuleBuilder" /> instance for method chaining.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Types in this collection are expected to come from typeof() expressions (e.g. source-generated collections) whose metadata is preserved by the trimmer.")]
    public CommandModuleBuilder Register(IEnumerable<Type> types)
    {
        ArgumentNullException.ThrowIfNull(types);

        foreach (var type in types)
        {
            if (!type.IsAssignableTo(typeof(IRegistrableCommandConstruct)))
            {
                throw new NotSupportedException($"The given type '{type.Name}' is not a command construct and cannot be registered.");
            }

            _messageRegistry.Register(type);
            _registeredTypes.Add(type);
        }

        return this;
    }

    /// <summary>
    ///     Registers all command types from the specified assembly that implement <see cref="IRegistrableCommandConstruct" />.
    /// </summary>
    /// <param name="assembly">The assembly from which to register command types.</param>
    /// <returns>The current <see cref="CommandModuleBuilder" /> instance for method chaining.</returns>
    /// <remarks>
    ///     This method uses <see cref="Assembly.GetTypes"/> which is not compatible with trimming or Native AOT.
    ///     Prefer using <see cref="Register(IEnumerable{Type})"/> with a source-generated type list for AOT scenarios.
    /// </remarks>
    [RequiresUnreferencedCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with trimming. Use Register(IEnumerable<Type>) with a source-generated type list instead.")]
    [RequiresDynamicCode("RegisterFromAssembly uses Assembly.GetTypes() which is not compatible with Native AOT. Use Register(IEnumerable<Type>) with a source-generated type list instead.")]
    public CommandModuleBuilder RegisterFromAssembly(Assembly assembly)
    {
        foreach (var registrableCommandConstruct in assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(IRegistrableCommandConstruct))))
        {
            _messageRegistry.Register(registrableCommandConstruct);
            _registeredTypes.Add(registrableCommandConstruct);
        }

        return this;
    }

    /// <summary>
    ///     Registers the set of command types that are decorated with
    ///     <see cref="LiteBus.Commands.Abstractions.StoreInInboxAttribute" />.
    ///     Pass <c>GeneratedLiteBusHandlers.InboxCommands</c> here for a fully AOT-safe, reflection-free setup.
    /// </summary>
    /// <param name="inboxCommandTypes">
    ///     The compile-time list of inbox command types.  Typically sourced from the
    ///     source-generated <c>GeneratedLiteBusHandlers.InboxCommands</c> property.
    /// </param>
    /// <returns>The current <see cref="CommandModuleBuilder" /> instance for method chaining.</returns>
    /// <example>
    /// <code>
    /// builder.RegisterInboxCommands(GeneratedLiteBusHandlers.InboxCommands);
    /// </code>
    /// </example>
    public CommandModuleBuilder RegisterInboxCommands(IReadOnlyList<Type> inboxCommandTypes)
    {
        ArgumentNullException.ThrowIfNull(inboxCommandTypes);
        _inboxCommandTypes.AddRange(inboxCommandTypes);
        return this;
    }

    /// <summary>Builds the <see cref="InboxCommandSet" /> from the types accumulated via <see cref="RegisterInboxCommands" />.</summary>
    internal InboxCommandSet BuildInboxCommandSet() => new(_inboxCommandTypes);

    /// <summary>
    ///     Gets the set of types that were requested to be registered by this builder instance.
    ///     Used by <see cref="CommandModule" /> to determine which handler descriptors belong
    ///     to this specific build call, regardless of global registry deduplication.
    /// </summary>
    internal HashSet<Type> GetRegisteredTypes() => _registeredTypes;
}
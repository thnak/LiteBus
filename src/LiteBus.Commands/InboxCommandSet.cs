using System;
using System.Collections.Generic;
using LiteBus.Commands.Abstractions;

namespace LiteBus.Commands;

/// <summary>
///     An AOT-safe, compile-time-built set of command types that are decorated with
///     <see cref="StoreInInboxAttribute" />.
///     Built from <c>GeneratedLiteBusHandlers.InboxCommands</c> and registered as a singleton by
///     <see cref="CommandModule" /> when the caller invokes
///     <see cref="CommandModuleBuilder.RegisterInboxCommands" />.
/// </summary>
internal sealed class InboxCommandSet : ICommandInboxTypeSet
{
    private readonly HashSet<Type> _types;

    internal InboxCommandSet(IReadOnlyList<Type> inboxCommandTypes)
    {
        _types = new HashSet<Type>(inboxCommandTypes);
    }

    /// <inheritdoc />
    public bool IsInboxCommand(Type commandType) => _types.Contains(commandType);
}

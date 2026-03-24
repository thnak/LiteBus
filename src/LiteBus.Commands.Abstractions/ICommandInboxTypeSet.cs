using System;

namespace LiteBus.Commands.Abstractions;

/// <summary>
///     Provides an AOT-safe, compile-time-built lookup of command types that are decorated
///     with <see cref="StoreInInboxAttribute" />.
///     The default implementation is built by <c>CommandModuleBuilder.RegisterInboxCommands</c>
///     and registered as a singleton by the command module.
/// </summary>
/// <remarks>
///     This interface exists to allow <see cref="ICommandMediator" /> to route inbox commands
///     without runtime <c>Attribute.GetCustomAttribute</c> reflection.
///     Pass <c>GeneratedLiteBusHandlers.InboxCommands</c> to
///     <c>CommandModuleBuilder.RegisterInboxCommands</c> to wire this up automatically.
/// </remarks>
public interface ICommandInboxTypeSet
{
    /// <summary>
    ///     Returns <see langword="true" /> when <paramref name="commandType" /> is registered
    ///     as an inbox command (i.e. it carries <see cref="StoreInInboxAttribute" />).
    /// </summary>
    /// <param name="commandType">The runtime type of the command being dispatched.</param>
    bool IsInboxCommand(Type commandType);
}

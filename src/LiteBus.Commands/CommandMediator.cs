using System;
using System.Threading;
using System.Threading.Tasks;
using LiteBus.Commands.Abstractions;
using LiteBus.Messaging.Abstractions;

namespace LiteBus.Commands;

/// <summary>
///     The primary implementation of <see cref="ICommandMediator" />. It orchestrates the command execution
///     pipeline, including diverting commands to be stored in the inbox if they are marked for durable processing.
/// </summary>
public sealed class CommandMediator : ICommandMediator
{
    private readonly ICommandInbox? _commandInbox;
    private readonly ICommandInboxTypeSet _inboxTypeSet;
    private readonly IMessageMediator _messageMediator;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CommandMediator" />.
    /// </summary>
    /// <param name="messageMediator">The core message mediator for immediate command execution.</param>
    /// <param name="commandInbox">The registered command inbox implementation. If null, the inbox feature is disabled.</param>
    /// <param name="inboxTypeSet">
    ///     The compile-time-built set of command types decorated with
    ///     <see cref="StoreInInboxAttribute" />.  Built from
    ///     <c>GeneratedLiteBusHandlers.InboxCommands</c> via
    ///     <see cref="CommandModuleBuilder.RegisterInboxCommands" /> and registered as a singleton
    ///     by <see cref="CommandModule" />.
    ///     When <see langword="null" /> (i.e. no inbox commands were registered) an empty set is used,
    ///     so no commands are ever routed to the inbox.
    /// </param>
    public CommandMediator(IMessageMediator messageMediator, ICommandInbox? commandInbox = null, ICommandInboxTypeSet? inboxTypeSet = null)
    {
        ArgumentNullException.ThrowIfNull(messageMediator);

        _messageMediator = messageMediator;
        _commandInbox = commandInbox;
        _inboxTypeSet = inboxTypeSet ?? new InboxCommandSet([]);
    }

    /// <inheritdoc />
    public Task SendAsync(ICommand command, CommandMediationSettings? commandMediationSettings = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if the command should be diverted to the inbox for durable processing.
        if (ShouldBeStoredInInbox(command.GetType(), commandMediationSettings))
        {
            // The command is stored for deferred execution by the background processor.
            return _commandInbox!.StoreAsync(command, cancellationToken);
        }

        // Proceed with immediate, in-process execution.
        commandMediationSettings ??= new CommandMediationSettings();
        var mediationStrategy = new SingleAsyncHandlerMediationStrategy<ICommand>();
        var findStrategy = new ActualTypeOrFirstAssignableTypeMessageResolveStrategy();

        var options = new MediateOptions<ICommand, Task>
        {
            MessageMediationStrategy = mediationStrategy,
            MessageResolveStrategy = findStrategy,
            CancellationToken = cancellationToken,
            Tags = commandMediationSettings.Filters.Tags,
            Items = commandMediationSettings.Items
        };

        return _messageMediator.Mediate(command, options);
    }

    /// <inheritdoc />
    public Task<TCommandResult> SendAsync<TCommandResult>(ICommand<TCommandResult> command,
                                                          CommandMediationSettings? commandMediationSettings = null,
                                                          CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Check if the command should be diverted to the inbox for durable processing.
        if (ShouldBeStoredInInbox(command.GetType(), commandMediationSettings))
        {
            // The command is stored for deferred execution.
            _commandInbox!.StoreAsync(command, cancellationToken);

            // Return a completed task with a default result. The caller should not expect
            // the actual result, as execution is now asynchronous. This is typically
            // paired with an API response like HTTP 202 (Accepted).
            return Task.FromResult(default(TCommandResult))!;
        }

        // Proceed with immediate, in-process execution.
        commandMediationSettings ??= new CommandMediationSettings();
        var mediationStrategy = new SingleAsyncHandlerMediationStrategy<ICommand<TCommandResult>, TCommandResult>();
        var findStrategy = new ActualTypeOrFirstAssignableTypeMessageResolveStrategy();

        var options = new MediateOptions<ICommand<TCommandResult>, Task<TCommandResult>>
        {
            MessageResolveStrategy = findStrategy,
            MessageMediationStrategy = mediationStrategy,
            CancellationToken = cancellationToken,
            Tags = commandMediationSettings.Filters.Tags,
            Items = commandMediationSettings.Items
        };

        return _messageMediator.Mediate(command, options);
    }

    /// <summary>
    ///     Determines if a command should be stored in the inbox for deferred processing.
    ///     Uses the compile-time-built <see cref="ICommandInboxTypeSet" /> for an O(1) AOT-safe lookup —
    ///     no runtime reflection or attribute scanning is performed.
    /// </summary>
    private bool ShouldBeStoredInInbox(Type commandType, CommandMediationSettings? settings)
    {
        // A command should not be stored again if it's already being processed from the inbox.
        if (settings?.Items.ContainsKey("IsInboxExecution") == true)
        {
            return false;
        }

        return _inboxTypeSet.IsInboxCommand(commandType);
    }
}

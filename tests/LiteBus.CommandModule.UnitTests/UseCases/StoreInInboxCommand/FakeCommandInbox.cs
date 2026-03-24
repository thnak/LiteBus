using LiteBus.Commands.Abstractions;

namespace LiteBus.CommandModule.UnitTests.UseCases.StoreInInboxCommand;

/// <summary>
///     An in-memory <see cref="ICommandInbox" /> stub used in tests to verify that commands
///     decorated with <see cref="StoreInInboxAttribute" /> are routed to the inbox.
/// </summary>
public sealed class FakeCommandInbox : ICommandInbox
{
    public List<ICommand> StoredCommands { get; } = new();

    public Task StoreAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        StoredCommands.Add(command);
        return Task.CompletedTask;
    }
}

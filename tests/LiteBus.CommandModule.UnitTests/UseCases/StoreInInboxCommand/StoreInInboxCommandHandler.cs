using LiteBus.Commands.Abstractions;

namespace LiteBus.CommandModule.UnitTests.UseCases.StoreInInboxCommand;

public sealed class StoreInInboxCommandHandler : ICommandHandler<StoreInInboxCommand>
{
    public Task HandleAsync(StoreInInboxCommand message, CancellationToken cancellationToken = default)
    {
        message.ExecutedTypes.Add(GetType());
        return Task.CompletedTask;
    }
}

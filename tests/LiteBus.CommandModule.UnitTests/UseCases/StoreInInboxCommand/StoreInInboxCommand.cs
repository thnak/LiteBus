using LiteBus.Commands.Abstractions;

namespace LiteBus.CommandModule.UnitTests.UseCases.StoreInInboxCommand;

[StoreInInbox]
public sealed class StoreInInboxCommand : ICommand
{
    public List<Type> ExecutedTypes { get; } = new();
}

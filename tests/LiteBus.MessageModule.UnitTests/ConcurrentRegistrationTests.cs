using LiteBus.Commands;
using LiteBus.Commands.Abstractions;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.MessageModule.UnitTests.Data.FakeCommand.Handlers;
using LiteBus.MessageModule.UnitTests.Data.FakeCommand.Messages;
using LiteBus.MessageModule.UnitTests.Data.FakeQuery.Handlers;
using LiteBus.MessageModule.UnitTests.Data.FakeQuery.Messages;
using LiteBus.Queries;
using LiteBus.Queries.Abstractions;
using LiteBus.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LiteBus.MessageModule.UnitTests;

/// <summary>
///     Tests that concurrent <c>AddLiteBus()</c> calls (as in MS Orleans multi-silo scenarios where each silo
///     configures its own DI container in parallel) correctly register handlers in every container.
///     Root cause: the global <c>MessageRegistryAccessor.Instance</c> singleton's <c>startIndex = Handlers.Count</c>
///     pattern was racy — if Silo 2 captured <c>startIndex</c> after Silo 1 had already populated the registry,
///     <c>Skip(startIndex)</c> would return an empty sequence and Silo 2's container would have no handlers.
/// </summary>
[Collection("Sequential")]
public sealed class ConcurrentRegistrationTests : LiteBusTestBase
{
    [Fact]
    public async Task AddLiteBus_CalledConcurrently_BothContainersResolveHandlers()
    {
        // Arrange – two independent service collections, simulating two Orleans silos.
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();

        // Act – configure both containers in parallel, the same way Orleans would.
        await Task.WhenAll(
            Task.Run(() => services1.AddLiteBus(modules =>
            {
                modules.AddCommandModule(b => b.RegisterFromAssembly(typeof(FakeCommandHandlerWithoutResult).Assembly));
                modules.AddQueryModule(b => b.RegisterFromAssembly(typeof(FakeQueryHandlerWithoutResult).Assembly));
            })),
            Task.Run(() => services2.AddLiteBus(modules =>
            {
                modules.AddCommandModule(b => b.RegisterFromAssembly(typeof(FakeCommandHandlerWithoutResult).Assembly));
                modules.AddQueryModule(b => b.RegisterFromAssembly(typeof(FakeQueryHandlerWithoutResult).Assembly));
            }))
        );

        var provider1 = services1.BuildServiceProvider();
        var provider2 = services2.BuildServiceProvider();

        // Assert – container 1 must be able to dispatch a command and a query.
        var commandMediator1 = provider1.GetRequiredService<ICommandMediator>();
        var queryMediator1 = provider1.GetRequiredService<IQueryMediator>();

        var cmd1 = new FakeCommand();
        await commandMediator1.SendAsync(cmd1);
        cmd1.ExecutedTypes.Should().Contain(typeof(FakeCommandHandlerWithoutResult));

        var qry1 = new FakeQuery();
        var result1 = await queryMediator1.QueryAsync(qry1);
        result1.Should().NotBeNull();
        qry1.ExecutedTypes.Should().Contain(typeof(FakeQueryHandlerWithoutResult));

        // Assert – container 2 must independently be able to dispatch the same message types.
        var commandMediator2 = provider2.GetRequiredService<ICommandMediator>();
        var queryMediator2 = provider2.GetRequiredService<IQueryMediator>();

        var cmd2 = new FakeCommand();
        await commandMediator2.SendAsync(cmd2);
        cmd2.ExecutedTypes.Should().Contain(typeof(FakeCommandHandlerWithoutResult));

        var qry2 = new FakeQuery();
        var result2 = await queryMediator2.QueryAsync(qry2);
        result2.Should().NotBeNull();
        qry2.ExecutedTypes.Should().Contain(typeof(FakeQueryHandlerWithoutResult));
    }
}

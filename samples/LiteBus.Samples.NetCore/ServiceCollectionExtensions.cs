using LiteBus.Commands;
using LiteBus.Events;
using LiteBus.Extensions.Microsoft.DependencyInjection;
using LiteBus.Queries;

namespace LiteBus.Samples.NetCore;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Order Processing example with Microsoft Dependency Injection.
    ///     This includes all commands, queries, event handlers, and infrastructure services.
    /// </summary>
    public static IServiceCollection AddLiteBusExample(this IServiceCollection services)
    {
        // AOT-safe registration: use compile-time-generated type lists from the source generator
        // instead of RegisterFromAssembly which requires reflection.
        services.AddLiteBus(liteBus =>
        {
            liteBus.AddCommandModule(module => module.Register(LiteBus.Generated.GeneratedLiteBusHandlers.CommandHandlers));
            liteBus.AddQueryModule(module => module.Register(LiteBus.Generated.GeneratedLiteBusHandlers.QueryHandlers));
            liteBus.AddEventModule(module => module.Register(LiteBus.Generated.GeneratedLiteBusHandlers.EventHandlers));
        });

        return services;
    }
}
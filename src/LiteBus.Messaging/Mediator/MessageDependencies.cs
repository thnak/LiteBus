using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Extensions;

namespace LiteBus.Messaging.Mediator;

/// <inheritdoc cref="IMessageDependencies" />
internal sealed class MessageDependencies : IMessageDependencies
{
    private readonly Func<IHandlerDescriptor, bool> _handlerPredicate;
    private readonly Type _messageType;
    private readonly IEnumerable<string> _tags;

    public MessageDependencies(Type messageType,
                               IMessageDescriptor descriptor,
                               IServiceProvider serviceProvider,
                               IEnumerable<string> tags,
                               Func<IHandlerDescriptor, bool> handlerPredicate)
    {
        _messageType = messageType;
        _tags = tags;
        _handlerPredicate = handlerPredicate;

        MainHandlers = ResolveHandlers(descriptor.Handlers, handlerType => (IMessageHandler) serviceProvider.GetRequiredService(handlerType));
        IndirectMainHandlers = ResolveHandlers(descriptor.IndirectHandlers, handlerType => (IMessageHandler) serviceProvider.GetRequiredService(handlerType));

        PreHandlers = ResolveHandlers(descriptor.PreHandlers, handlerType => (IMessagePreHandler) serviceProvider.GetRequiredService(handlerType));
        IndirectPreHandlers = ResolveHandlers(descriptor.IndirectPreHandlers, handlerType => (IMessagePreHandler) serviceProvider.GetRequiredService(handlerType));

        PostHandlers = ResolveHandlers(descriptor.PostHandlers, handlerType => (IMessagePostHandler) serviceProvider.GetRequiredService(handlerType));
        IndirectPostHandlers = ResolveHandlers(descriptor.IndirectPostHandlers, handlerType => (IMessagePostHandler) serviceProvider.GetRequiredService(handlerType));

        ErrorHandlers = ResolveHandlers(descriptor.ErrorHandlers, handlerType => (IMessageErrorHandler) serviceProvider.GetRequiredService(handlerType));
        IndirectErrorHandlers = ResolveHandlers(descriptor.IndirectErrorHandlers, handlerType => (IMessageErrorHandler) serviceProvider.GetRequiredService(handlerType));
    }

    public ILazyHandlerCollection<IMessageHandler, IMainHandlerDescriptor> MainHandlers { get; }

    public ILazyHandlerCollection<IMessageHandler, IMainHandlerDescriptor> IndirectMainHandlers { get; }

    public ILazyHandlerCollection<IMessagePreHandler, IPreHandlerDescriptor> PreHandlers { get; }

    public ILazyHandlerCollection<IMessagePreHandler, IPreHandlerDescriptor> IndirectPreHandlers { get; }

    public ILazyHandlerCollection<IMessagePostHandler, IPostHandlerDescriptor> PostHandlers { get; }

    public ILazyHandlerCollection<IMessagePostHandler, IPostHandlerDescriptor> IndirectPostHandlers { get; }

    public ILazyHandlerCollection<IMessageErrorHandler, IErrorHandlerDescriptor> ErrorHandlers { get; }

    public ILazyHandlerCollection<IMessageErrorHandler, IErrorHandlerDescriptor> IndirectErrorHandlers { get; }

    /// <summary>
    ///     Resolves handlers from the provided descriptors and a handler resolution function.
    /// </summary>
    private ILazyHandlerCollection<THandler, TDescriptor> ResolveHandlers<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] THandler, TDescriptor>(
        IEnumerable<TDescriptor> descriptors,
        Func<Type, THandler> resolveFunc) where TDescriptor : IHandlerDescriptor
    {
        return descriptors
            .Where(d => _handlerPredicate(d))
            .Where(d => d.Tags.Count == 0 || d.Tags.Intersect(_tags).Any())
            .OrderBy(d => d.Priority)
            .Select(d => new LazyHandler<THandler, TDescriptor>
            {
                Handler = new Lazy<THandler>(() => resolveFunc(GetHandlerType(d))),
                Descriptor = d
            })
            .ToLazyReadOnlyCollection();
    }

    /// <summary>
    ///     Retrieves the handler type from a descriptor, adjusting for generic message types as necessary.
    ///     When the registered handler is an open generic type definition (e.g. <c>SomeHandler&lt;&gt;</c>
    ///     for <c>SomeMessage&lt;T&gt;</c>), it is closed with the actual type arguments of the runtime
    ///     message.  This path is only exercised when a handler type definition is registered manually
    ///     via <c>Register(typeof(SomeHandler&lt;&gt;))</c>; the source generator always emits already-
    ///     closed types so <c>MakeGenericType</c> is not called at all in the AOT-safe path.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "handlerType is only a generic type definition when Register(typeof(OpenHandler<>)) was called explicitly. " +
                        "The source generator emits closed typeof() forms, so this branch is never reached in the AOT-safe path.")]
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Same as IL2055: this branch is unreachable when using the source-generated registration path.")]
    private Type GetHandlerType(IHandlerDescriptor descriptor)
    {
        var handlerType = descriptor.HandlerType;

        // Only close the generic type when both the message type is generic AND the stored handler
        // is itself an open generic type definition (e.g. SomeHandler<>).  When the source generator
        // is used, handlerType is always already closed, so IsGenericTypeDefinition is false and
        // MakeGenericType is never called.
        if (descriptor.MessageType.IsGenericType && handlerType.IsGenericTypeDefinition)
        {
            handlerType = handlerType.MakeGenericType(_messageType.GetGenericArguments());
        }

        return handlerType;
    }
}
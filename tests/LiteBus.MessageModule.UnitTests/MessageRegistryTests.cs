using LiteBus.Commands.Abstractions;
using LiteBus.Events.Abstractions;
using LiteBus.Messaging.Abstractions;
using LiteBus.Messaging.Registry;
using LiteBus.Testing;

namespace LiteBus.MessageModule.UnitTests;

[Collection("Sequential")]
public sealed class MessageRegistryTests : LiteBusTestBase
{
    // Test data types
    public enum CustomEnum
    {
        One,
        Two,
        Three
    }

    [Fact]
    public void Register_RecordClass_ShouldRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestRecordClass));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(TestRecordClass));
    }

    [Fact]
    public void Register_RecordStruct_ShouldRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestRecordStruct));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(TestRecordStruct));
    }

    [Fact]
    public void Register_RegularClass_ShouldRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestClass));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(TestClass));
    }

    [Fact]
    public void Register_RegularStruct_ShouldRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestStruct));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(TestStruct));
    }

    [Fact]
    public void Register_CustomValueType_ShouldRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(CustomEnum));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(CustomEnum));
    }

    [Fact]
    public void Register_SystemType_ShouldNotRegisterAsMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(string));
        registry.Register(typeof(int));
        registry.Register(typeof(DateTime));

        // Assert
        registry.Should().BeEmpty();
    }

    [Fact]
    public void Register_SameTypeMultipleTimes_ShouldRegisterOnce()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestRecordClass));
        registry.Register(typeof(TestRecordClass));
        registry.Register(typeof(TestRecordClass));

        // Assert
        registry.Should().HaveCount(1);
    }

    [Fact]
    public void Register_GenericRecordStruct_ShouldRegisterGenericTypeDefinition()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(GenericRecordStruct<string>));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(GenericRecordStruct<>));
    }

    [Fact]
    public void Register_GenericRecordClass_ShouldRegisterGenericTypeDefinition()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(GenericRecordClass<int>));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(GenericRecordClass<>));
    }

    [Fact]
    public void Register_Handler_ShouldRegisterHandlerAndMessage()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Act
        registry.Register(typeof(TestHandler));

        // Assert
        registry.Should().HaveCount(1);
        registry.First().MessageType.Should().Be(typeof(TestRecordStruct));

        // Handler should be registered with the message
        var messageDescriptor = registry.First();
        messageDescriptor.Handlers.Should().HaveCount(1);
        messageDescriptor.Handlers.First().HandlerType.Should().Be(typeof(TestHandler));
    }

    public record TestRecordClass(string Name) : IEvent;

    public readonly record struct TestRecordStruct(string Name) : IEvent;

    public class TestClass : IEvent
    {
        public required string Name { get; set; }
    }

    public struct TestStruct
    {
        public string Name { get; set; }
    }

    public record GenericRecordClass<T>(T Value) : IEvent;

    public readonly record struct GenericRecordStruct<T>(T Value) : IEvent;

    public class TestHandler : IAsyncMessageHandler<TestRecordStruct>
    {
        public Task HandleAsync(TestRecordStruct message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // --- Open Generic Handler Test Types ---

    public class TestCommand : ICommand;

    public class AnotherTestCommand : ICommand;

    public class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task HandleAsync(TestCommand message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    public class OpenGenericTestPreHandler<T> : ICommandPreHandler<T> where T : ICommand
    {
        public Task PreHandleAsync(T message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // --- Open Generic Handler Tests ---

    [Fact]
    public void Register_ClosedGenericPreHandler_ShouldLinkToConcreteMessageType()
    {
        // Arrange — register the concrete handler first; the source generator would emit
        // typeof(OpenGenericTestPreHandler<TestCommand>) rather than the open generic form.
        var registry = new MessageRegistry();
        registry.Register(typeof(TestCommandHandler));

        // Act — register the closed form as the source generator emits it
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));

        // Assert
        var messageDescriptor = registry.Single(d => d.MessageType == typeof(TestCommand));
        messageDescriptor.PreHandlers.Should().HaveCount(1);
        messageDescriptor.PreHandlers.First().HandlerType.Should().Be(typeof(OpenGenericTestPreHandler<TestCommand>));
    }

    [Fact]
    public void Register_ConcreteMessageAfterClosedGenericPreHandler_ShouldLink()
    {
        // Arrange — register the closed form before the command handler;
        // registration order does not matter for closed forms.
        var registry = new MessageRegistry();
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));

        // Act
        registry.Register(typeof(TestCommandHandler));

        // Assert
        var messageDescriptor = registry.Single(d => d.MessageType == typeof(TestCommand));
        messageDescriptor.PreHandlers.Should().HaveCount(1);
        messageDescriptor.PreHandlers.First().HandlerType.Should().Be(typeof(OpenGenericTestPreHandler<TestCommand>));
    }

    [Fact]
    public void Register_ClosedGenericPreHandler_ShouldApplyToMultipleConcreteMessageTypes()
    {
        // Arrange — source generator emits one closed form per message type.
        var registry = new MessageRegistry();
        registry.Register(typeof(TestCommandHandler));
        registry.Register(typeof(AnotherTestCommand));

        // Act
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));
        registry.Register(typeof(OpenGenericTestPreHandler<AnotherTestCommand>));

        // Assert
        var testCommandDescriptor = registry.Single(d => d.MessageType == typeof(TestCommand));
        testCommandDescriptor.PreHandlers.Should().HaveCount(1);
        testCommandDescriptor.PreHandlers.First().HandlerType.Should().Be(typeof(OpenGenericTestPreHandler<TestCommand>));

        var anotherCommandDescriptor = registry.Single(d => d.MessageType == typeof(AnotherTestCommand));
        anotherCommandDescriptor.PreHandlers.Should().HaveCount(1);
        anotherCommandDescriptor.PreHandlers.First().HandlerType.Should().Be(typeof(OpenGenericTestPreHandler<AnotherTestCommand>));
    }

    [Fact]
    public void Register_OpenGenericHandler_ShouldNotApplyToTypesNotSatisfyingConstraints()
    {
        // Arrange
        var registry = new MessageRegistry();

        // Register a non-ICommand event type
        registry.Register(typeof(TestRecordClass));

        var countBeforeOpenGeneric = registry.Count;

        // Act - registering an open generic type definition is silently ignored by the registry;
        // only the closed form for the constrained type would be emitted by the source generator.
        registry.Register(typeof(OpenGenericTestPreHandler<>));

        // Assert - the event type should not have the command pre-handler
        var eventDescriptor = registry.Single(d => d.MessageType == typeof(TestRecordClass));
        eventDescriptor.PreHandlers.Should().BeEmpty();

        // The silently-ignored open-generic registration must not add any new message descriptors.
        registry.Count.Should().Be(countBeforeOpenGeneric);
    }

    [Fact]
    public void Register_ClosedGenericHandlerTwice_ShouldOnlyRegisterOnce()
    {
        // Arrange
        var registry = new MessageRegistry();
        registry.Register(typeof(TestCommandHandler));

        // Act — registering the same closed form twice should be idempotent
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));

        // Assert
        var messageDescriptor = registry.Single(d => d.MessageType == typeof(TestCommand));
        messageDescriptor.PreHandlers.Should().HaveCount(1);
    }

    [Fact]
    public void Clear_ShouldClearRegisteredHandlers()
    {
        // Arrange — use the closed form as the source generator would emit.
        var registry = new MessageRegistry();
        registry.Register(typeof(OpenGenericTestPreHandler<TestCommand>));
        registry.Register(typeof(TestCommandHandler));

        // Verify it was registered
        registry.Single(d => d.MessageType == typeof(TestCommand)).PreHandlers.Should().HaveCount(1);

        // Act
        registry.Clear();

        // Register again without the pre-handler
        registry.Register(typeof(TestCommandHandler));

        // Assert — pre-handler should not be present after Clear
        var descriptor = registry.Single(d => d.MessageType == typeof(TestCommand));
        descriptor.PreHandlers.Should().BeEmpty();
    }
}
namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using Shouldly;

using ViscaCamLink.ViewModels;

using Xunit;

public sealed class ViewModelBaseTests
{
    [Fact]
    public async Task TryCameraOperation_Success()
    {
        static Task act() => TestViewModel.RunOperation(Task.CompletedTask);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task TryCameraOperation_WhenOperationThrows_SwallowsException()
    {
        var faultedOperation = Task.FromException(new InvalidOperationException("camera error"));

        Task act() => TestViewModel.RunOperation(faultedOperation);

        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task RunSafeAsync_WhenOperationThrows_LogsAndSwallowsException()
    {
        var logger = new Mock<ILogger>();
        var operationException = new InvalidOperationException("command error");

        logger.Setup(log => log.IsEnabled(LogLevel.Error)).Returns(true);

        Task act() => TestViewModel.RunSafeOperation(
            () => Task.FromException(operationException),
            logger.Object,
            "TestCommand");

        await Should.NotThrowAsync(act);

        logger.Verify(
            logged => logged.Log(
                LogLevel.Error,
                It.Is<EventId>(eventId => eventId.Id == 2901),
                It.Is<It.IsAnyType>((state, _) => state != null && state.ToString()!.Contains("TestCommand")),
                It.Is<Exception>(exception => exception == operationException),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void NotifyPropertyChanged_Success()
    {
        var viewModel = new TestViewModel();
        string? changedPropertyName = null;

        viewModel.PropertyChanged += (_, args) => changedPropertyName = args.PropertyName;

        viewModel.Notify("SomeProperty");

        changedPropertyName.ShouldBe("SomeProperty");
    }

    private sealed class TestViewModel : ViewModelBase
    {
        public static Task RunOperation(Task operation) => TryCameraOperation(operation);

        public static Task RunSafeOperation(Func<Task> operation, ILogger logger, string operationName) =>
            RunSafeAsync(operation, logger, operationName);

        public void Notify(string propertyName) => NotifyPropertyChanged(propertyName);
    }
}

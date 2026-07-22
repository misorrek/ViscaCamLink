namespace ViscaCamLink.Tests.ViewModels;

using System;
using System.Threading.Tasks;

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

        public void Notify(string propertyName) => NotifyPropertyChanged(propertyName);
    }
}

namespace ViscaCamLink.Tests.Infrastructure.Interface;

using Shouldly;

using ViscaCamLink.Infrastructure.Interface;

using Xunit;

public sealed class CommandTests
{
    [Fact]
    public void Execute_Success()
    {
        var executed = false;
        var command = new Command(() => executed = true);

        command.Execute(null);

        executed.ShouldBeTrue();
    }

    [Fact]
    public void Execute_WhenActionTakesParameter_PassesParameter()
    {
        object? receivedParameter = null;
        var command = new Command(parameter => receivedParameter = parameter);

        command.Execute("the parameter");

        receivedParameter.ShouldBe("the parameter");
    }

    [Fact]
    public void CanExecute_Success()
    {
        var command = new Command(() => { });

        command.CanExecute(null).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanExecute_WhenFuncIsGiven_ReturnsFuncResult(bool funcResult)
    {
        var command = new Command(() => { }, () => funcResult);

        command.CanExecute(null).ShouldBe(funcResult);
    }

    [Fact]
    public void Execute_WhenCanExecuteReturnsFalse_DoesNotExecute()
    {
        var executed = false;
        var command = new Command(() => executed = true, () => false);

        command.Execute(null);

        executed.ShouldBeFalse();
    }

    [Fact]
    public void Invalidate_Success()
    {
        var command = new Command(() => { });
        var raised = false;

        command.CanExecuteChanged += (_, _) => raised = true;

        command.Invalidate();

        raised.ShouldBeTrue();
    }
}

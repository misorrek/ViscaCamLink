namespace ViscaCamLink.Tests.ViewModels;

using Shouldly;

using ViscaCamLink.ViewModels;

using Xunit;

public sealed class MigrationViewModelTests
{
    [Fact]
    public void ParseUninstallString_QuotedPathWithArguments()
    {
        var result = MigrationViewModel.ParseUninstallString("\"C:\\Program Files\\Legacy\\uninstall.exe\" /quiet /old");

        result.Executable.ShouldBe("C:\\Program Files\\Legacy\\uninstall.exe");
        result.Arguments.ShouldBe("/quiet /old");
    }

    [Fact]
    public void ParseUninstallString_UnquotedPathWithArguments()
    {
        var result = MigrationViewModel.ParseUninstallString("C:\\Legacy\\uninstall.exe /quiet");

        result.Executable.ShouldBe("C:\\Legacy\\uninstall.exe");
        result.Arguments.ShouldBe("/quiet");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"")]
    [InlineData("\"\" /quiet")]
    [InlineData("\"C:\\Legacy\\uninstall.exe")]
    public void ParseUninstallString_EmptyOrInvalid_ReturnsEmpty(string uninstallString)
    {
        var result = MigrationViewModel.ParseUninstallString(uninstallString);

        result.Executable.ShouldBeEmpty();
        result.Arguments.ShouldBeEmpty();
    }
}

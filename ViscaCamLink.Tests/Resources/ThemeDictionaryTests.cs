namespace ViscaCamLink.Tests.Resources;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

using Xunit;

public sealed class ThemeDictionaryTests
{
    private const string AppProjectFolder = "ViscaCamLink";
    private const string AppProjectFile = "ViscaCamLink.csproj";
    private const string LightThemeFile = "LightTheme.xaml";
    private const string DarkThemeFile = "DarkTheme.xaml";

    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static readonly Regex HexColorPattern = new("\"#[0-9A-Fa-f]{3,8}\"", RegexOptions.Compiled);
    private static readonly Regex BrushReferencePattern = new(@"\{(?:Dynamic|Static)Resource (\w+Brush)\}", RegexOptions.Compiled);
    private static readonly Regex StaticBrushReferencePattern = new(@"\{StaticResource \w+Brush\}", RegexOptions.Compiled);

    private readonly string _resourcesDirectory;
    private readonly string _appDirectory;

    public ThemeDictionaryTests()
    {
        _appDirectory = FindAppDirectory();
        _resourcesDirectory = Path.Combine(_appDirectory, "Resources");
    }

    [Fact]
    public void ThemeKeys_Success()
    {
        var lightKeys = LoadKeys(LightThemeFile);
        var darkKeys = LoadKeys(DarkThemeFile);

        lightKeys.ShouldNotBeEmpty();
        lightKeys.Except(darkKeys).ShouldBeEmpty();
        darkKeys.Except(lightKeys).ShouldBeEmpty();
    }

    [Fact]
    public void ThemeKeys_WhenReferencedInXaml_ExistInThemes()
    {
        var themeKeys = LoadKeys(LightThemeFile);
        var referencedKeys = GetAppXamlFiles()
            .SelectMany(file => BrushReferencePattern.Matches(File.ReadAllText(file)).Select(match => match.Groups[1].Value))
            .Distinct();

        referencedKeys.Except(themeKeys).ShouldBeEmpty();
    }

    [Fact]
    public void AppXaml_WhenOutsideThemes_HasNoHexColorsOrStaticBrushReferences()
    {
        var offendingFiles = GetAppXamlFiles()
            .Where(file => HexColorPattern.IsMatch(File.ReadAllText(file)) || StaticBrushReferencePattern.IsMatch(File.ReadAllText(file)))
            .Select(Path.GetFileName);

        offendingFiles.ShouldBeEmpty();
    }

    private HashSet<string> LoadKeys(string themeFile)
    {
        var document = XDocument.Load(Path.Combine(_resourcesDirectory, themeFile));

        return document.Root!.Elements()
            .Select(element => (string?)element.Attribute(XamlNamespace + "Key"))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    private IEnumerable<string> GetAppXamlFiles()
    {
        return Directory.EnumerateFiles(_appDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(file => !file.EndsWith(LightThemeFile, StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(DarkThemeFile, StringComparison.OrdinalIgnoreCase));
    }

    private static string FindAppDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, AppProjectFolder);

            if (File.Exists(Path.Combine(candidate, AppProjectFile)))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"{AppProjectFolder}/{AppProjectFile} not found above {AppContext.BaseDirectory}");
    }
}

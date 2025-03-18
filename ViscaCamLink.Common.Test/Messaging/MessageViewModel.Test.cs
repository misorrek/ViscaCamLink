namespace ViscaCamLink.Common.Test.Messaging;

using System.Windows;
using System.Windows.Media;

using ViscaCamLink.Common.Messaging;

public class MessageViewModelTest
{
    private MessageViewModel MessageViewModel { get; set; } = null!;

    [SetUp]
    public void SetUp()
    {
        MessageViewModel = new MessageViewModel();
    }

    [Test]
    public void Initialize()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MessageViewModel.OkCommand, Is.Not.Null);
            Assert.That(MessageViewModel.CancelCommand, Is.Not.Null);
            Assert.That(MessageViewModel.Title, Is.Empty);
            Assert.That(MessageViewModel.Text, Is.Empty);
            Assert.That(MessageViewModel.Icon, Is.EqualTo(Geometry.Empty));
            Assert.That(MessageViewModel.IconBackgroundColor, Is.EqualTo(Color.FromArgb(0, 0, 0, 0)));
            Assert.That(MessageViewModel.OkButtonVisible, Is.False);
            Assert.That(MessageViewModel.OkButtonText, Is.Empty);
            Assert.That(MessageViewModel.CancelButtonVisible, Is.False);
            Assert.That(MessageViewModel.CancelButtonText, Is.Empty);
            Assert.That(MessageViewModel.Image, Is.EqualTo(MessageBoxImage.None));
            Assert.That(MessageViewModel.Button, Is.EqualTo(MessageBoxButton.OK));
            Assert.That(MessageViewModel.Result, Is.EqualTo(MessageBoxResult.None));
        });
    }

    [TestCase(MessageBoxImage.Information)]    
    [TestCase(MessageBoxImage.Question)]
    [TestCase(MessageBoxImage.Warning)]
    [TestCase(MessageBoxImage.Error)]
    [TestCase(MessageBoxImage.Asterisk)]
    [TestCase(MessageBoxImage.Exclamation)]
    [TestCase(MessageBoxImage.Hand)]
    [TestCase(MessageBoxImage.Stop)]
    public void UpdateImage(MessageBoxImage image)
    {
        MessageViewModel.Image = image;

        Assert.Multiple(() =>
        {
            Assert.That(MessageViewModel.Image, Is.EqualTo(image));
            Assert.That(MessageViewModel.Icon, Is.Not.EqualTo(Geometry.Empty));
            Assert.That(MessageViewModel.IconBackgroundColor, Is.Not.EqualTo(Color.FromArgb(0, 0, 0, 0)));
        });

        MessageViewModel.Image = MessageBoxImage.None;

        Assert.Multiple(() =>
        {
            Assert.That(MessageViewModel.Image, Is.EqualTo(MessageBoxImage.None));
            Assert.That(MessageViewModel.Icon, Is.EqualTo(Geometry.Empty));
            Assert.That(MessageViewModel.IconBackgroundColor, Is.EqualTo(Color.FromArgb(0, 0, 0, 0)));
        });
    }

    [TestCase(MessageBoxButton.OK)]
    public void UpdateButton_OneButton(MessageBoxButton button)
    {        
        MessageViewModel.Button = button;

        Assert.Multiple(() =>
        {
            Assert.That(MessageViewModel.Button, Is.EqualTo(button));
            Assert.That(MessageViewModel.OkButtonVisible, Is.True);
            Assert.That(MessageViewModel.CancelButtonVisible, Is.False);
        });
    }

    [TestCase(MessageBoxButton.OKCancel)]
    [TestCase(MessageBoxButton.YesNo)]
    public void UpdateButton_TwoButtons(MessageBoxButton button)
    {
        MessageViewModel.Button = button;

        Assert.Multiple(() =>
        {
            Assert.That(MessageViewModel.Button, Is.EqualTo(button));
            Assert.That(MessageViewModel.OkButtonVisible, Is.True);
            Assert.That(MessageViewModel.CancelButtonVisible, Is.True);
        });
    }

    [TestCase(MessageBoxButton.YesNoCancel)]
    public void UpdateButton_NotImplemented(MessageBoxButton button)
    {
        Assert.Throws<NotImplementedException>(() => MessageViewModel.Button = button);
    }

    [Test]
    public void OkCommand()
    {
        Assert.That(MessageViewModel.OkCommand.CanExecute(null), Is.True);

        MessageViewModel.OkCommand.Execute(null);

        Assert.That(MessageViewModel.Result, Is.EqualTo(MessageBoxResult.OK));
    }

    [Test]
    public void CancelCommand()
    {
        Assert.That(MessageViewModel.CancelCommand.CanExecute(null), Is.True);

        MessageViewModel.CancelCommand.Execute(null);

        Assert.That(MessageViewModel.Result, Is.EqualTo(MessageBoxResult.Cancel));
    }
}
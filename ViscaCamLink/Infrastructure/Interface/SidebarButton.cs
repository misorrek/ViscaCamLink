namespace ViscaCamLink.Infrastructure.Interface;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public class SidebarButton : Button
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Geometry),
        typeof(SidebarButton),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(SidebarButton),
        new PropertyMetadata(false));

    public Geometry? Icon
    {
        get => (Geometry?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }
}

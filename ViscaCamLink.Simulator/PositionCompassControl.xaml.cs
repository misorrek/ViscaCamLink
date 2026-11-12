namespace ViscaCamLink.Simulator;

using System;
using System.Windows;
using System.Windows.Controls;

public partial class PositionCompassControl : UserControl
{
    private const double CanvasCenter = 100.0;
    private const double CompassRadius = 90.0;
    private const double DotOffset = 5.0;
    private const double ArrowLength = 80.0;
    private const double MinVisibleVelocity = 0.5;

    public PositionCompassControl()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty PanNormalizedProperty =
        DependencyProperty.Register(nameof(PanNormalized), typeof(double), typeof(PositionCompassControl),
            new PropertyMetadata(0.0, OnPositionChanged));

    public static readonly DependencyProperty TiltNormalizedProperty =
        DependencyProperty.Register(nameof(TiltNormalized), typeof(double), typeof(PositionCompassControl),
            new PropertyMetadata(0.0, OnPositionChanged));

    public static readonly DependencyProperty ZoomNormalizedProperty =
        DependencyProperty.Register(nameof(ZoomNormalized), typeof(double), typeof(PositionCompassControl),
            new PropertyMetadata(0.0, OnZoomChanged));

    public static readonly DependencyProperty PanVelocityProperty =
        DependencyProperty.Register(nameof(PanVelocity), typeof(int), typeof(PositionCompassControl),
            new PropertyMetadata(0, OnVelocityChanged));

    public static readonly DependencyProperty TiltVelocityProperty =
        DependencyProperty.Register(nameof(TiltVelocity), typeof(int), typeof(PositionCompassControl),
            new PropertyMetadata(0, OnVelocityChanged));

    public double PanNormalized
    {
        get => (double)GetValue(PanNormalizedProperty);
        set => SetValue(PanNormalizedProperty, value);
    }

    public double TiltNormalized
    {
        get => (double)GetValue(TiltNormalizedProperty);
        set => SetValue(TiltNormalizedProperty, value);
    }

    public double ZoomNormalized
    {
        get => (double)GetValue(ZoomNormalizedProperty);
        set => SetValue(ZoomNormalizedProperty, value);
    }

    public int PanVelocity
    {
        get => (int)GetValue(PanVelocityProperty);
        set => SetValue(PanVelocityProperty, value);
    }

    public int TiltVelocity
    {
        get => (int)GetValue(TiltVelocityProperty);
        set => SetValue(TiltVelocityProperty, value);
    }

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl control)
        {
            control.UpdatePosition();
        }
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl control)
        {
            control.UpdateZoom();
        }
    }

    private static void OnVelocityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl control)
        {
            control.UpdateVelocity();
        }
    }

    private void UpdatePosition()
    {
        var x = PanNormalized * CompassRadius;
        var y = -TiltNormalized * CompassRadius; // positive tilt = up physically = negative Y on screen

        PositionTransform.X = CanvasCenter - DotOffset + x;
        PositionTransform.Y = CanvasCenter - DotOffset + y;
    }

    private void UpdateZoom()
    {
        ZoomRing.Opacity = ZoomNormalized;
    }

    private void UpdateVelocity()
    {
        double velocityX = PanVelocity;
        double velocityY = TiltVelocity;
        var magnitude = Math.Sqrt(velocityX * velocityX + velocityY * velocityY);

        if (magnitude < MinVisibleVelocity)
        {
            VelocityLine.Visibility = Visibility.Collapsed;

            return;
        }

        VelocityLine.Visibility = Visibility.Visible;

        // Same uniform scale for both axes; negate Y because screen Y is inverted
        var scale = Math.Min(magnitude / ViscaConstants.MaxPanSpeed, 1.0) * ArrowLength;

        VelocityLine.X1 = CanvasCenter;
        VelocityLine.Y1 = CanvasCenter;
        VelocityLine.X2 = CanvasCenter + velocityX / magnitude * scale;
        VelocityLine.Y2 = CanvasCenter - velocityY / magnitude * scale;
    }
}

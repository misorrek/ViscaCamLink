namespace ViscaCamLink.Simulator;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public partial class PositionCompassControl : UserControl
{
    public PositionCompassControl()
    {
        InitializeComponent();
    }

    // --- Dependency Properties ---

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

    // --- Update Logic ---

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl ctrl) ctrl.UpdatePosition();
    }

    private static void OnZoomChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl ctrl) ctrl.UpdateZoom();
    }

    private static void OnVelocityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PositionCompassControl ctrl) ctrl.UpdateVelocity();
    }

    private void UpdatePosition()
    {
        // Map normalized pan (-1..1) and tilt (-1..1) to canvas coordinates
        // Center is (100, 100), radius is 90
        double x = PanNormalized * 90.0;
        double y = -TiltNormalized * 90.0; // positive tilt = up physically = negative Y on screen

        PositionTransform.X = 95 + x; // 95 = center(100) - dot_radius(5)
        PositionTransform.Y = 95 + y;
    }

    private void UpdateZoom()
    {
        // Show zoom as opacity of the outer ring
        ZoomRing.Opacity = ZoomNormalized;
    }

    private void UpdateVelocity()
    {
        double vx = PanVelocity;
        double vy = TiltVelocity; // positive tilt velocity = moving toward MaxTilt = "up" physically = dot moves up = negative screen Y

        double magnitude = Math.Sqrt(vx * vx + vy * vy);

        if (magnitude < 0.5)
        {
            VelocityLine.Visibility = Visibility.Collapsed;
            return;
        }

        VelocityLine.Visibility = Visibility.Visible;

        // Arrow direction matches dot movement: same uniform scale for both axes
        // Negate Y because screen Y is inverted (positive tilt = up = negative screen Y)
        double maxSpeed = ViscaConstants.MaxPanSpeed;
        double scale = Math.Min(magnitude / maxSpeed, 1.0) * 80.0;
        double nx = vx / magnitude;
        double ny = -vy / magnitude; // negate for screen coordinates

        VelocityLine.X1 = 100;
        VelocityLine.Y1 = 100;
        VelocityLine.X2 = 100 + nx * scale;
        VelocityLine.Y2 = 100 + ny * scale;
    }
}

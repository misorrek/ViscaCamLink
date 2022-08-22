namespace ViscaCamLink.Util
{
    using System.Windows;
    using System.Windows.Media;

    public class SidebarButtonExtension
    {
        public static DependencyProperty BorderVisibilityProperty = DependencyProperty.RegisterAttached("BorderVisibility",
                                                                                                        typeof(Visibility),
                                                                                                        typeof(SidebarButtonExtension),
                                                                                                        new PropertyMetadata());

        public static DependencyProperty PathDataProperty = DependencyProperty.RegisterAttached("PathData",
                                                                                                typeof(Geometry),
                                                                                                typeof(SidebarButtonExtension),
                                                                                                new PropertyMetadata());

        public static Visibility GetBorderVisibility(DependencyObject target)
        {
            return (Visibility)target.GetValue(BorderVisibilityProperty);
        }

        public static void SetBorderVisibility(DependencyObject target, Visibility value)
        {
            target.SetValue(BorderVisibilityProperty, value);
        }

        public static Geometry GetPathData(DependencyObject target)
        {
            return (Geometry)target.GetValue(PathDataProperty);
        }

        public static void SetPathData(DependencyObject target, Geometry value)
        {
            target.SetValue(PathDataProperty, value);
        }
    }
}

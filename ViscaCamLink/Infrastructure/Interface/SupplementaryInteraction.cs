namespace ViscaCamLink.Infrastructure.Interface;

using System.Collections.Generic;
using System.Windows;

using Microsoft.Xaml.Behaviors;
using TriggerBase = Microsoft.Xaml.Behaviors.TriggerBase;

public class Behaviors : List<Behavior> { }

public class Triggers : List<TriggerBase> { }

public static class SupplementaryInteraction
{
    public static readonly DependencyProperty BehaviorsProperty = DependencyProperty.RegisterAttached(
        "Behaviors", 
        typeof(Behaviors), 
        typeof(SupplementaryInteraction), 
        new UIPropertyMetadata(null, OnPropertyBehaviorsChanged));

    public static readonly DependencyProperty TriggersProperty = DependencyProperty.RegisterAttached(
        "Triggers",
        typeof(Triggers),
        typeof(SupplementaryInteraction),
        new UIPropertyMetadata(null, OnPropertyTriggersChanged));

    public static Behaviors GetBehaviors(DependencyObject dependencyObject)
    {
        return (Behaviors)dependencyObject.GetValue(BehaviorsProperty);
    }

    public static void SetBehaviors(DependencyObject dependencyObject, Behaviors value)
    {
        dependencyObject.SetValue(BehaviorsProperty, value);
    }

    public static Triggers GetTriggers(DependencyObject dependencyObject)
    {
        return (Triggers)dependencyObject.GetValue(TriggersProperty);
    }

    public static void SetTriggers(DependencyObject dependencyObject, Triggers value)
    {
        dependencyObject.SetValue(TriggersProperty, value);
    }
    
    private static void OnPropertyBehaviorsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var behaviors = Interaction.GetBehaviors(dependencyObject);

        if (eventArgs.NewValue is not Behaviors eventBehaviors)
        {
            return;
        }

        foreach (var behavior in eventBehaviors)
        {
            behaviors.Add(behavior);
        }
    }

    private static void OnPropertyTriggersChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        var triggers = Interaction.GetTriggers(dependencyObject);

        if (eventArgs.NewValue is not Triggers newTriggers)
        {
            return;
        }

        foreach (var trigger in newTriggers)
        {
            triggers.Add(trigger);
        }
    }
}

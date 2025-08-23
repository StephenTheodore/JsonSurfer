using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.Toolkit.PropertyGrid;

namespace JsonSurfer.App.Behaviors;

public class PropertyGridBehavior : Behavior<PropertyGrid>
{
    public static readonly DependencyProperty PropertyValueChangedCommandProperty =
        DependencyProperty.Register(
            nameof(PropertyValueChangedCommand),
            typeof(ICommand),
            typeof(PropertyGridBehavior),
            new PropertyMetadata(null));

    public ICommand? PropertyValueChangedCommand
    {
        get => (ICommand?)GetValue(PropertyValueChangedCommandProperty);
        set => SetValue(PropertyValueChangedCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PropertyValueChanged += OnPropertyValueChanged;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.PropertyValueChanged -= OnPropertyValueChanged;
        }
        base.OnDetaching();
    }

    private void OnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
    {
        PropertyValueChangedCommand?.Execute(e);
    }
}
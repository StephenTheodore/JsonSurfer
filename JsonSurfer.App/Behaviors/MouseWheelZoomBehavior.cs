using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;

namespace JsonSurfer.App.Behaviors;

public class MouseWheelZoomBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty ZoomInCommandProperty =
        DependencyProperty.Register(
            nameof(ZoomInCommand),
            typeof(ICommand),
            typeof(MouseWheelZoomBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ZoomOutCommandProperty =
        DependencyProperty.Register(
            nameof(ZoomOutCommand),
            typeof(ICommand),
            typeof(MouseWheelZoomBehavior),
            new PropertyMetadata(null));

    public ICommand? ZoomInCommand
    {
        get => (ICommand?)GetValue(ZoomInCommandProperty);
        set => SetValue(ZoomInCommandProperty, value);
    }

    public ICommand? ZoomOutCommand
    {
        get => (ICommand?)GetValue(ZoomOutCommandProperty);
        set => SetValue(ZoomOutCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
        base.OnDetaching();
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            
            if (e.Delta > 0)
            {
                ZoomInCommand?.Execute(null);
            }
            else
            {
                ZoomOutCommand?.Execute(null);
            }
        }
    }
}
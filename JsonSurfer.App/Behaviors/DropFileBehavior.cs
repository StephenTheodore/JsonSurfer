using Microsoft.Xaml.Behaviors;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using JsonSurfer.App.ViewModels;

namespace JsonSurfer.App.Behaviors;

public class DropFileBehavior : Behavior<FrameworkElement>
{
    public static readonly DependencyProperty DropCommandProperty =
        DependencyProperty.Register(
            nameof(DropCommand),
            typeof(ICommand),
            typeof(DropFileBehavior),
            new PropertyMetadata(null));

    public ICommand? DropCommand
    {
        get => (ICommand?)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject != null)
        {
            AssociatedObject.AllowDrop = true;
            AssociatedObject.DragEnter += OnDragEnter;
            AssociatedObject.DragOver += OnDragOver;
            AssociatedObject.Drop += OnDrop;
        }
    }

    protected override void OnDetaching()
    {
        if (AssociatedObject != null)
        {
            AssociatedObject.DragEnter -= OnDragEnter;
            AssociatedObject.DragOver -= OnDragOver;
            AssociatedObject.Drop -= OnDrop;
        }
        base.OnDetaching();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        HandleDragEvent(e);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        HandleDragEvent(e);
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            DropCommand?.Execute(files);
        }
        e.Handled = true;
    }

    private void HandleDragEvent(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            // Use static method to check if files are valid
            bool canAccept = files.Any(MainViewModel.IsValidJsonFile);
            
            e.Effects = canAccept ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }
}
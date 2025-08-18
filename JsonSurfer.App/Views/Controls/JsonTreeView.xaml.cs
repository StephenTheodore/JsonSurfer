using System.Windows;
using System.Windows.Controls;
using JsonSurfer.App.ViewModels;
using JsonSurfer.Core.Models;

namespace JsonSurfer.App.Views.Controls;

public partial class JsonTreeView : UserControl
{
    public JsonTreeView()
    {
        InitializeComponent();
    }

    private void JsonTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel && e.NewValue is JsonNode selectedNode)
        {
            viewModel.SelectedNode = selectedNode;
        }
    }
}
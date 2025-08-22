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
        
        // Subscribe to TreeView events for expansion state tracking
        JsonTree.Loaded += JsonTree_Loaded;
    }

    private void JsonTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MainViewModel viewModel && e.NewValue is JsonNode selectedNode)
        {
            viewModel.SelectedNode = selectedNode;
        }
    }

    private void JsonTree_Loaded(object sender, RoutedEventArgs e)
    {
        // Attach expansion state change handlers to TreeViewItems
        AttachExpansionHandlers();
    }

    private void AttachExpansionHandlers()
    {
        // Use ItemContainerGenerator to access TreeViewItems after they're generated
        JsonTree.ItemContainerGenerator.StatusChanged += (s, e) =>
        {
            if (JsonTree.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                AttachExpansionHandlersToItems();
            }
        };
    }

    private void AttachExpansionHandlersToItems()
    {
        if (JsonTree.ItemsSource == null) return;

        foreach (var item in JsonTree.ItemsSource)
        {
            var container = JsonTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
            if (container != null)
            {
                AttachExpansionHandlerRecursive(container);
            }
        }
    }

    private void AttachExpansionHandlerRecursive(TreeViewItem item)
    {
        // Remove existing handlers to avoid duplicates
        item.Expanded -= TreeViewItem_Expanded;
        item.Collapsed -= TreeViewItem_Collapsed;
        
        // Add new handlers
        item.Expanded += TreeViewItem_Expanded;
        item.Collapsed += TreeViewItem_Collapsed;

        // Process child items
        for (int i = 0; i < item.Items.Count; i++)
        {
            var childContainer = item.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
            if (childContainer != null)
            {
                AttachExpansionHandlerRecursive(childContainer);
            }
        }
    }

    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is JsonNode node)
        {
            node.IsExpanded = true;
        }
    }

    private void TreeViewItem_Collapsed(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.DataContext is JsonNode node)
        {
            node.IsExpanded = false;
        }
    }
}
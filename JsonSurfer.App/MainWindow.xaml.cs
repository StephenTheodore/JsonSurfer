using System.Windows;
using System.Windows.Controls; // Added for SelectionChangedEventArgs
using JsonSurfer.App.ViewModels;

namespace JsonSurfer.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl tabControl && tabControl.SelectedItem is TabItem selectedTab)
        {
            if (DataContext is MainViewModel viewModel)
            {
                if (selectedTab.Header.ToString() == "Code Editor")
                {
                    // Switching to Code Editor tab
                    viewModel.UpdateTextFromVisuals();
                }
                else if (selectedTab.Header.ToString() == "Visual Editor")
                {
                    // Switching to Visual Editor tab
                    viewModel.UpdateVisualsFromText();
                }
            }
        }
    }
}
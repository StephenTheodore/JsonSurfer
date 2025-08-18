using System.Windows;
using System.Windows.Input;
using JsonSurfer.App.ViewModels;
using JsonSurfer.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using JsonSurfer.App.Messages;

namespace JsonSurfer.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void ErrorList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGrid dataGrid && dataGrid.SelectedItem is ValidationError error)
        {
            // Send message to JsonCodeEditor to navigate to the error line
            WeakReferenceMessenger.Default.Send(new JsonErrorOccurredMessage(
                new JsonErrorDetails(error.Message, error.Line, error.Column)));
        }
    }
}
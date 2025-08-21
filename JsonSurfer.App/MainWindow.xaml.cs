using System.Windows;
using System.Windows.Input;
using JsonSurfer.App.ViewModels;
using JsonSurfer.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using JsonSurfer.App.Messages;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Media;
using System.Windows.Controls;

namespace JsonSurfer.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly CompareViewModel _compareViewModel;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;
        
        // Get CompareViewModel from DI
        _compareViewModel = serviceProvider.GetRequiredService<CompareViewModel>();
        
        // Setup drag and drop event handlers
        Drop += MainWindow_Drop;
        DragEnter += MainWindow_DragEnter;
        DragOver += MainWindow_DragOver;
        
        // Set Compare tab DataContext
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Find the Compare TabItem and set its DataContext
        if (MainTabControl.Items.Count > 2 && MainTabControl.Items[2] is System.Windows.Controls.TabItem compareTab)
        {
            compareTab.DataContext = _compareViewModel;
        }
        
        // Setup Compare tab drag and drop
        SetupCompareTabDragAndDrop();
    }

    private void SetupCompareTabDragAndDrop()
    {
        // Make sure panels allow drop
        LeftComparePanel.AllowDrop = true;
        RightComparePanel.AllowDrop = true;
        
        // Left panel drag and drop
        LeftComparePanel.DragEnter += LeftComparePanel_DragEnter;
        LeftComparePanel.DragOver += LeftComparePanel_DragOver;
        LeftComparePanel.Drop += LeftComparePanel_Drop;
        
        // Right panel drag and drop
        RightComparePanel.DragEnter += RightComparePanel_DragEnter;
        RightComparePanel.DragOver += RightComparePanel_DragOver;
        RightComparePanel.Drop += RightComparePanel_Drop;
    }

    private void LeftComparePanel_DragEnter(object sender, DragEventArgs e)
    {
        HandleComparePanelDrag(e);
    }

    private void LeftComparePanel_DragOver(object sender, DragEventArgs e)
    {
        HandleComparePanelDrag(e);
    }

    private async void LeftComparePanel_Drop(object sender, DragEventArgs e)
    {
        var jsonFile = GetFirstValidJsonFileFromDrop(e);
        if (jsonFile != null)
        {
            await _compareViewModel.LoadLeftFileFromDrop(jsonFile);
        }
        e.Handled = true;
    }

    private void RightComparePanel_DragEnter(object sender, DragEventArgs e)
    {
        HandleComparePanelDrag(e);
    }

    private void RightComparePanel_DragOver(object sender, DragEventArgs e)
    {
        HandleComparePanelDrag(e);
    }

    private async void RightComparePanel_Drop(object sender, DragEventArgs e)
    {
        var jsonFile = GetFirstValidJsonFileFromDrop(e);
        if (jsonFile != null)
        {
            await _compareViewModel.LoadRightFileFromDrop(jsonFile);
        }
        e.Handled = true;
    }

    private void HandleComparePanelDrag(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            e.Effects = files.Any(IsValidJsonFile) ? DragDropEffects.Copy : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private string? GetFirstValidJsonFileFromDrop(DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            return files.FirstOrDefault(IsValidJsonFile);
        }
        return null;
    }

    private void ErrorList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGrid dataGrid && dataGrid.SelectedItem is JsonSurfer.Core.Models.ProblemItem problem)
        {
            // Send message to JsonCodeEditor to navigate to the error line
            WeakReferenceMessenger.Default.Send(new JsonErrorOccurredMessage(
                new JsonErrorDetails(problem.Message, problem.Line, problem.Column)));
        }
    }

    private void CompareList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.DataGrid dataGrid && dataGrid.SelectedItem is JsonSurfer.Core.Models.ValidationError difference)
        {
            // Update ViewModel with highlighted line
            _compareViewModel.HighlightLine(difference.Line);
            
            // For Compare tab differences, we need to highlight the line in both text boxes
            // Since they are simple TextBox controls, we'll scroll to the line
            ScrollToLineInCompareTextBoxes(difference.Line);
        }
    }

    private void ScrollToLineInCompareTextBoxes(int lineNumber)
    {
        try 
        {
            // Find the TextBox controls in the Compare tab
            var compareTabItem = MainTabControl.Items[2] as System.Windows.Controls.TabItem;
            if (compareTabItem?.Content is Grid compareGrid)
            {
                // Find left and right TextBox controls
                var leftTextBox = FindCompareTextBox(compareGrid, "LeftComparePanel");
                var rightTextBox = FindCompareTextBox(compareGrid, "RightComparePanel");

                if (leftTextBox != null && rightTextBox != null)
                {
                    // Highlight and scroll to the target line in both editors
                    HighlightLineInTextBox(leftTextBox, lineNumber, System.Windows.Media.Brushes.LightBlue);
                    HighlightLineInTextBox(rightTextBox, lineNumber, System.Windows.Media.Brushes.LightCoral);
                    
                    // Focus on left editor for user interaction
                    leftTextBox.Focus();
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback - just show a message if navigation fails
            System.Diagnostics.Debug.WriteLine($"Failed to navigate to line {lineNumber}: {ex.Message}");
        }
    }

    private System.Windows.Controls.TextBox? FindCompareTextBox(Grid compareGrid, string panelName)
    {
        var panel = FindChild<Grid>(compareGrid, panelName);
        return FindChild<System.Windows.Controls.TextBox>(panel);
    }

    private void HighlightLineInTextBox(System.Windows.Controls.TextBox textBox, int lineNumber, System.Windows.Media.Brush highlightColor)
    {
        if (textBox == null || string.IsNullOrEmpty(textBox.Text)) return;

        var lines = textBox.Text.Split('\n');
        
        if (lineNumber > 0 && lineNumber <= lines.Length)
        {
            // Calculate start and end positions for the target line
            int startIndex = lines.Take(lineNumber - 1).Sum(line => line.Length + 1); // +1 for \n
            int lineLength = lines[lineNumber - 1].Length;
            
            // Select the entire line to highlight it
            textBox.Select(startIndex, lineLength);
            
            // Scroll to make sure the line is visible
            ScrollToCharacterPosition(textBox, startIndex);
            
            // Change background color temporarily
            var originalBackground = textBox.Background;
            textBox.Background = highlightColor;
            
            // Reset background after 3 seconds
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, e) => 
            {
                textBox.Background = originalBackground;
                timer.Stop();
            };
            timer.Start();
        }
    }

    private void ScrollToCharacterPosition(System.Windows.Controls.TextBox textBox, int characterIndex)
    {
        try
        {
            // Get the line index from character position
            int lineIndex = textBox.GetLineIndexFromCharacterIndex(characterIndex);
            
            // Scroll to that line
            textBox.ScrollToLine(lineIndex);
        }
        catch
        {
            // If scrolling fails, just ignore it
        }
    }

    private T? FindChild<T>(DependencyObject parent, string? childName = null) where T : DependencyObject
    {
        if (parent == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            
            if (child is T t && (childName == null || (child is FrameworkElement fe && fe.Name == childName)))
            {
                return t;
            }

            var childOfChild = FindChild<T>(child, childName);
            if (childOfChild != null)
                return childOfChild;
        }
        
        return null;
    }

    private void MainWindow_DragEnter(object sender, DragEventArgs e)
    {
        // Check if the data object contains file list
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            // Check if any of the files has valid JSON extension
            if (files.Any(file => IsValidJsonFile(file)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        
        e.Handled = true;
    }

    private void MainWindow_DragOver(object sender, DragEventArgs e)
    {
        // Same logic as DragEnter
        MainWindow_DragEnter(sender, e);
    }

    private async void MainWindow_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            
            // Get the first valid JSON file
            var jsonFile = files.FirstOrDefault(file => IsValidJsonFile(file));
            
            if (jsonFile != null)
            {
                try
                {
                    // Use the ViewModel's file service to read the file
                    var content = await File.ReadAllTextAsync(jsonFile);
                    
                    // Update ViewModel properties
                    _viewModel.JsonContent = content;
                    _viewModel.CurrentFilePath = jsonFile;
                    _viewModel.WindowTitle = $"JsonSurfer - {Path.GetFileName(jsonFile)}";
                    _viewModel.IsModified = false;
                    
                    // Auto-validate the loaded JSON
                    _viewModel.ValidateJsonCommand.Execute(null);
                    
                    // Switch to Code Editor tab to show the loaded content
                    _viewModel.SelectedTabIndex = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load file: {ex.Message}", "Drag & Drop Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        e.Handled = true;
    }

    private static bool IsValidJsonFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return false;
            
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".json" || extension == ".info";
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using JsonSurfer.App.Messages;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace JsonSurfer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IJsonParserService _jsonParserService;
    private readonly IValidationService _validationService;
    private readonly IFileService _fileService;

    [ObservableProperty]
    private string _jsonContent = string.Empty;

    [ObservableProperty]
    private JsonNode? _rootNode;

    [ObservableProperty]
    private JsonNode? _selectedNode; // Added this property

    [ObservableProperty]
    private ValidationResult? _validationResult;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "JsonSurfer - JSON Editor";

    [ObservableProperty]
    private int _selectedTabIndex;

    public MainViewModel(IJsonParserService jsonParserService, IValidationService validationService, IFileService fileService)
    {
        _jsonParserService = jsonParserService;
        _validationService = validationService;
        _fileService = fileService;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Open JSON File",
                Filter = "JSON Files (*.json;*.info)|*.json;*.info|All Files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var content = await _fileService.ReadFileAsync(openFileDialog.FileName);
                JsonContent = content;
                CurrentFilePath = openFileDialog.FileName;
                WindowTitle = $"JsonSurfer - {Path.GetFileName(openFileDialog.FileName)}";
                IsModified = false;

                // Auto-validate when opening
                ValidateJson();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        try
        {
            string filePath = CurrentFilePath;

            if (string.IsNullOrEmpty(filePath))
            {
                await SaveAsFileAsync();
                return;
            }

            var success = await _fileService.WriteFileAsync(filePath, JsonContent);
            if (success)
            {
                IsModified = false;
                MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Failed to save file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsFileAsync()
    {
        try
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save JSON File As",
                Filter = "JSON Files (*.json)|*.json|Info Files (*.info)|*.info|All Files (*.*)|*.*",
                DefaultExt = "json",
                FileName = string.IsNullOrEmpty(CurrentFilePath) ? "untitled.json" : Path.GetFileName(CurrentFilePath)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var success = await _fileService.WriteFileAsync(saveFileDialog.FileName, JsonContent);
                if (success)
                {
                    CurrentFilePath = saveFileDialog.FileName;
                    WindowTitle = $"JsonSurfer - {Path.GetFileName(saveFileDialog.FileName)}";
                    IsModified = false;
                    MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save file as: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Exit()
    {
        if (IsModified)
        {
            var result = MessageBox.Show("You have unsaved changes. Do you want to save before exiting?", 
                "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                SaveFileAsync().Wait(); // Wait for save to complete
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return; // Don't exit
            }
        }

        Application.Current.Shutdown();
    }

    [RelayCommand]
    private void ValidateJson()
    {
        if (!string.IsNullOrEmpty(JsonContent))
        {
            ValidationResult = _jsonParserService.ValidateJson(JsonContent);
            RootNode = _jsonParserService.ParseToTree(JsonContent);
        }
    }

    // Automatically mark as modified when content changes and validate
    partial void OnJsonContentChanged(string value)
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            IsModified = true;
        }
        
        // Auto-validate on content change
        ValidateJson();
    }

    // Handle tab changes through property change notification
    partial void OnSelectedTabIndexChanged(int value)
    {
        System.Diagnostics.Debug.WriteLine($"Tab changed to index: {value}");
        
        // 0 = Code Editor, 1 = Visual Editor
        if (value == 0) // Code Editor selected
        {
            System.Diagnostics.Debug.WriteLine("Switching to Code Editor - UpdateTextFromVisuals");
            UpdateTextFromVisuals();
        }
        else if (value == 1) // Visual Editor selected  
        {
            System.Diagnostics.Debug.WriteLine("Switching to Visual Editor - UpdateVisualsFromText");
            UpdateVisualsFromText();
        }
    }

    // Added these methods for bidirectional synchronization
    public void UpdateTextFromVisuals()
    {
        if (RootNode != null)
        {
            try
            {
                JsonContent = _jsonParserService.SerializeFromTree(RootNode);
                ValidationResult = _jsonParserService.ValidateJson(JsonContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error serializing JSON from tree: {ex.Message}", "Serialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Optionally, revert to previous JsonContent or show a clear error state
            }
        }
    }

    public void UpdateVisualsFromText()
    {
        if (!string.IsNullOrEmpty(JsonContent))
        {
            ValidationResult = _jsonParserService.ValidateJson(JsonContent);
            if (ValidationResult.IsValid)
            {
                RootNode = _jsonParserService.ParseToTree(JsonContent);
            }
            else
            {
                // If JSON is invalid, clear the visual tree to avoid displaying incorrect data
                RootNode = null;
                SelectedNode = null;
                MessageBox.Show($"Invalid JSON: {ValidationResult.Errors.FirstOrDefault()?.Message}", "JSON Parsing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            RootNode = null;
            SelectedNode = null;
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;
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
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "Save JSON File",
                    Filter = "JSON Files (*.json)|*.json|Info Files (*.info)|*.info|All Files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    filePath = saveFileDialog.FileName;
                }
                else
                {
                    return; // User cancelled
                }
            }

            var success = await _fileService.WriteFileAsync(filePath, JsonContent);
            if (success)
            {
                CurrentFilePath = filePath;
                WindowTitle = $"JsonSurfer - {Path.GetFileName(filePath)}";
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

    // Automatically mark as modified when content changes
    partial void OnJsonContentChanged(string value)
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            IsModified = true;
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
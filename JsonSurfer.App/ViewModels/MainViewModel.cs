using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;
using CommunityToolkit.Mvvm.Messaging;
using JsonSurfer.App.Messages;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

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
    private List<ProblemItem> _allProblems = [];

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "JsonSurfer - JSON Editor";

    [ObservableProperty]
    private int _selectedTabIndex;

    private bool _isUpdatingFromTree = false;


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

            // If we're on Visual Editor tab and have RootNode changes, 
            // sync from tree to JSON content first
            if (SelectedTabIndex == 1 && RootNode != null)
            {
                System.Diagnostics.Debug.WriteLine("Saving from Visual Editor - syncing tree to JSON");
                UpdateTextFromVisuals();
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
                // If we're on Visual Editor tab and have RootNode changes, 
                // sync from tree to JSON content first
                if (SelectedTabIndex == 1 && RootNode != null)
                {
                    System.Diagnostics.Debug.WriteLine("Save As from Visual Editor - syncing tree to JSON");
                    UpdateTextFromVisuals();
                }

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
    private void FormatJson()
    {
        if (!string.IsNullOrEmpty(JsonContent))
        {
            try
            {
                var formattedContent = _jsonParserService.FormatJson(JsonContent);
                JsonContent = formattedContent;
                // ValidationResult will be updated automatically via OnJsonContentChanged
                MessageBox.Show("JSON formatted successfully!", "Format Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to format JSON: {ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void ValidateJson()
    {
        if (!string.IsNullOrEmpty(JsonContent))
        {
            try
            {
                ValidationResult = _jsonParserService.ValidateJsonWithAutoFix(JsonContent);
                RootNode = _jsonParserService.ParseToTree(JsonContent);
            }
            catch (Exception ex)
            {
                // Create error validation result if service fails
                ValidationResult = new ValidationResult
                {
                    IsValid = false,
                    Errors = [new ValidationError
                    {
                        Message = $"Validation service error: {ex.Message}",
                        Line = 0,
                        Column = 0,
                        Type = ErrorType.SyntaxError
                    }]
                };
                RootNode = null;
            }
        }
        else
        {
            // Clear validation result when content is empty
            ValidationResult = new ValidationResult { IsValid = true };
            RootNode = null;
        }
    }


    // Automatically mark as modified when content changes and validate
    partial void OnJsonContentChanged(string value)
    {
        if (!string.IsNullOrEmpty(CurrentFilePath))
        {
            IsModified = true;
        }
        
        // Auto-validate on content change, but skip tree rebuild if updating from tree
        if (!_isUpdatingFromTree)
        {
            ValidateJson();
        }
        else
        {
            // Just validate without rebuilding tree when updating from PropertyGrid
            if (!string.IsNullOrEmpty(JsonContent))
            {
                ValidationResult = _jsonParserService.ValidateJsonWithAutoFix(JsonContent);
            }
        }
    }

    // Handle validation result changes to update problems list
    partial void OnValidationResultChanged(ValidationResult? value)
    {
        UpdateAllProblems();
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

    private void UpdateAllProblems()
    {
        var problems = new List<ProblemItem>();

        if (ValidationResult != null)
        {
            // Add errors
            problems.AddRange(ValidationResult.Errors.Select(error => new ProblemItem
            {
                Message = error.Message,
                Line = error.Line,
                Column = error.Column,
                Type = error.Type.ToString(),
                IsError = true
            }));

            // Add warnings
            problems.AddRange(ValidationResult.Warnings.Select(warning => new ProblemItem
            {
                Message = warning.Message,
                Line = warning.Line,
                Column = warning.Column,
                Type = warning.Type.ToString(),
                IsError = false
            }));
        }

        AllProblems = problems;
    }

    // Public method for PropertyGrid to update JSON content without rebuilding tree
    public void UpdateJsonContentFromTree()
    {
        if (RootNode != null)
        {
            try
            {
                _isUpdatingFromTree = true;
                JsonContent = _jsonParserService.SerializeFromTree(RootNode);
                System.Diagnostics.Debug.WriteLine("JSON content updated from tree without validation");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to serialize tree: {ex.Message}");
            }
            finally
            {
                _isUpdatingFromTree = false;
            }
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
                ValidationResult = _jsonParserService.ValidateJsonWithAutoFix(JsonContent);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error serializing JSON from tree: {ex.Message}", "Serialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Create error validation result to show the serialization error
                ValidationResult = new ValidationResult
                {
                    IsValid = false,
                    Errors = [new ValidationError
                    {
                        Message = $"Serialization error: {ex.Message}",
                        Line = 0,
                        Column = 0,
                        Type = ErrorType.InvalidFormat
                    }]
                };
            }
        }
    }

    public void UpdateVisualsFromText()
    {
        if (!string.IsNullOrEmpty(JsonContent))
        {
            try
            {
                ValidationResult = _jsonParserService.ValidateJsonWithAutoFix(JsonContent);
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
            catch (Exception ex)
            {
                // Create error validation result if service fails
                ValidationResult = new ValidationResult
                {
                    IsValid = false,
                    Errors = [new ValidationError
                    {
                        Message = $"Parsing service error: {ex.Message}",
                        Line = 0,
                        Column = 0,
                        Type = ErrorType.SyntaxError
                    }]
                };
                RootNode = null;
                SelectedNode = null;
                MessageBox.Show($"Parsing error: {ex.Message}", "JSON Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            ValidationResult = new ValidationResult { IsValid = true };
            RootNode = null;
            SelectedNode = null;
        }
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace JsonSurfer.App.ViewModels;

public partial class CompareViewModel : ObservableObject
{
    private readonly IFileService _fileService;

    [ObservableProperty]
    private string _leftFilePath = string.Empty;

    [ObservableProperty]
    private string _rightFilePath = string.Empty;

    [ObservableProperty]
    private string _leftJsonContent = string.Empty;

    [ObservableProperty]
    private string _rightJsonContent = string.Empty;

    [ObservableProperty]
    private string _leftFormattedContent = string.Empty;

    [ObservableProperty]
    private string _rightFormattedContent = string.Empty;

    [ObservableProperty]
    private bool _hasComparison = false;

    [ObservableProperty]
    private string _comparisonResult = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ValidationError> _differences = new();

    [ObservableProperty]
    private int _highlightedLine = -1;

    public CompareViewModel(IFileService fileService)
    {
        _fileService = fileService;
    }

    [RelayCommand]
    private async Task LoadLeftFileAsync()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load Left JSON File",
                Filter = "JSON Files (*.json;*.info)|*.json;*.info|All Files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var content = await _fileService.ReadFileAsync(openFileDialog.FileName);
                LeftJsonContent = content;
                LeftFilePath = openFileDialog.FileName;
                
                // Auto-format and validate
                FormatLeftJson();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load left file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task LoadRightFileAsync()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Load Right JSON File",
                Filter = "JSON Files (*.json;*.info)|*.json;*.info|All Files (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var content = await _fileService.ReadFileAsync(openFileDialog.FileName);
                RightJsonContent = content;
                RightFilePath = openFileDialog.FileName;
                
                // Auto-format and validate
                FormatRightJson();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load right file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void CompareJson()
    {
        if (string.IsNullOrEmpty(LeftJsonContent) || string.IsNullOrEmpty(RightJsonContent))
        {
            MessageBox.Show("Please load both left and right JSON files before comparing.", "Compare Error", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Format both JSONs and compare
        FormatLeftJson();
        FormatRightJson();

        // Clear previous differences
        Differences.Clear();

        // Check if both files are formatted correctly
        if (string.IsNullOrEmpty(LeftFormattedContent) || string.IsNullOrEmpty(RightFormattedContent))
        {
            ComparisonResult = "Cannot compare: One or both files have formatting errors";
            HasComparison = true;
            return;
        }

        // Perform line-by-line comparison
        var leftLines = LeftFormattedContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var rightLines = RightFormattedContent.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        int maxLines = Math.Max(leftLines.Length, rightLines.Length);
        int differencesFound = 0;

        for (int i = 0; i < maxLines; i++)
        {
            string leftLine = i < leftLines.Length ? leftLines[i] : "";
            string rightLine = i < rightLines.Length ? rightLines[i] : "";

            if (!leftLine.Equals(rightLine, StringComparison.OrdinalIgnoreCase))
            {
                differencesFound++;
                
                if (i >= leftLines.Length)
                {
                    // Line exists only in right file
                    Differences.Add(new ValidationError
                    {
                        Type = ErrorType.InvalidFormat,
                        Line = i + 1,
                        Column = 1,
                        Message = $"Right: Added line - {rightLine.Trim()}"
                    });
                }
                else if (i >= rightLines.Length)
                {
                    // Line exists only in left file
                    Differences.Add(new ValidationError
                    {
                        Type = ErrorType.InvalidFormat,
                        Line = i + 1,
                        Column = 1,
                        Message = $"Left: Removed line - {leftLine.Trim()}"
                    });
                }
                else
                {
                    // Line differs between files
                    Differences.Add(new ValidationError
                    {
                        Type = ErrorType.InvalidFormat,
                        Line = i + 1,
                        Column = 1,
                        Message = $"Modified - Left: '{leftLine.Trim()}' → Right: '{rightLine.Trim()}'"
                    });
                }

                // Limit to prevent too many differences
                if (differencesFound >= 100)
                {
                    Differences.Add(new ValidationError
                    {
                        Type = ErrorType.InvalidValue,
                        Line = i + 2,
                        Column = 1,
                        Message = "Too many differences found. Showing first 100 differences only."
                    });
                    break;
                }
            }
        }

        bool areIdentical = differencesFound == 0;
        ComparisonResult = areIdentical ? 
            "Files are identical" : 
            $"Found {differencesFound} difference(s). See Problems panel below for details.";
        
        HasComparison = true;

        if (!areIdentical)
        {
            MessageBox.Show($"Found {differencesFound} difference(s).\nCheck the Problems panel below to navigate to specific differences.", 
                "Comparison Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Files are identical!", "Comparison Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    private void ClearComparison()
    {
        LeftFilePath = string.Empty;
        RightFilePath = string.Empty;
        LeftJsonContent = string.Empty;
        RightJsonContent = string.Empty;
        LeftFormattedContent = string.Empty;
        RightFormattedContent = string.Empty;
        HasComparison = false;
        ComparisonResult = string.Empty;
        Differences.Clear();
        HighlightedLine = -1;
    }

    public async Task LoadLeftFileFromDrop(string filePath)
    {
        try
        {
            var content = await _fileService.ReadFileAsync(filePath);
            LeftJsonContent = content;
            LeftFilePath = filePath;
            FormatLeftJson();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load dropped file: {ex.Message}", "Drag & Drop Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async Task LoadRightFileFromDrop(string filePath)
    {
        try
        {
            var content = await _fileService.ReadFileAsync(filePath);
            RightJsonContent = content;
            RightFilePath = filePath;
            FormatRightJson();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load dropped file: {ex.Message}", "Drag & Drop Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void FormatLeftJson()
    {
        LeftFormattedContent = FormatJsonContent(LeftJsonContent);
    }

    private void FormatRightJson()
    {
        RightFormattedContent = FormatJsonContent(RightJsonContent);
    }

    private string FormatJsonContent(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
            return string.Empty;

        try
        {
            var jsonDocument = JsonDocument.Parse(jsonContent);
            return JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }
        catch (JsonException)
        {
            return "❌ JSON Format Error"; // Return error indicator instead of empty
        }
    }

    public void HighlightLine(int lineNumber)
    {
        HighlightedLine = lineNumber;
    }

    public void ClearHighlight()
    {
        HighlightedLine = -1;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;

namespace JsonSurfer.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IJsonParserService _jsonParserService;

    [ObservableProperty]
    private string _jsonContent = string.Empty;

    [ObservableProperty]
    private JsonNode? _rootNode;

    [ObservableProperty]
    private ValidationResult? _validationResult;

    [ObservableProperty]
    private bool _isModified;

    public MainViewModel(IJsonParserService jsonParserService)
    {
        _jsonParserService = jsonParserService;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        // TODO: Implement file opening logic
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        // TODO: Implement file saving logic
        await Task.CompletedTask;
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
}
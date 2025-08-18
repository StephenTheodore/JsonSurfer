using System.Windows;
using System.Windows.Controls;
using JsonSurfer.App.ViewModels;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.Json;
using JsonSurfer.App.Helpers;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Messaging;
using JsonSurfer.App.Messages;

namespace JsonSurfer.App.Views.Controls;

public partial class JsonCodeEditor : UserControl, IRecipient<JsonErrorOccurredMessage>
{
    private FoldingManager? _foldingManager;
    private TextMarkerService? _textMarkerService;
    
    public JsonCodeEditor()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded; // Added for message unregistration
        SetupContextMenu();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Setup folding
        if (_foldingManager == null)
        {
            _foldingManager = FoldingManager.Install(JsonTextEditor.TextArea);
        }

        // Setup text marker service for error highlighting
        if (_textMarkerService == null)
        {
            _textMarkerService = new TextMarkerService(JsonTextEditor.Document);
            JsonTextEditor.TextArea.TextView.BackgroundRenderers.Add(_textMarkerService);
        }

        // Register for messages
        WeakReferenceMessenger.Default.Register(this);
        
        // Setup data binding through DataContext
        SetupDataBinding();
    }

    private void SetupDataBinding()
    {
        // Wait for DataContext to be set
        if (DataContext is MainViewModel viewModel)
        {
            // Clear any existing error highlighting when content changes
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.JsonContent))
                {
                    ClearErrorHighlighting();
                    if (JsonTextEditor.Text != viewModel.JsonContent)
                    {
                        JsonTextEditor.Text = viewModel.JsonContent;
                        UpdateFolding();
                    }
                }
            };

            // Bind JsonContent to AvalonEdit
            JsonTextEditor.Text = viewModel.JsonContent;

            JsonTextEditor.TextChanged += (s, e) =>
            {
                if (viewModel.JsonContent != JsonTextEditor.Text)
                {
                    viewModel.JsonContent = JsonTextEditor.Text;
                    UpdateFolding();
                }
            };
            
            UpdateFolding();
        }
        else
        {
            // If DataContext is not set yet, wait for it
            DataContextChanged += OnDataContextChanged;
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel)
        {
            DataContextChanged -= OnDataContextChanged;
            SetupDataBinding();
        }
    }

    private void SetupContextMenu()
    {
        var contextMenu = new ContextMenu();
        
        var formatItem = new MenuItem { Header = "Format JSON" };
        formatItem.Click += FormatJson_Click;
        contextMenu.Items.Add(formatItem);
        
        var validateItem = new MenuItem { Header = "Validate JSON" };
        validateItem.Click += ValidateJson_Click;
        contextMenu.Items.Add(validateItem);
        
        contextMenu.Items.Add(new Separator());
        
        var saveItem = new MenuItem { Header = "Save" };
        saveItem.Click += Save_Click;
        contextMenu.Items.Add(saveItem);
        
        var saveAsItem = new MenuItem { Header = "Save As..." };
        saveAsItem.Click += SaveAs_Click;
        contextMenu.Items.Add(saveAsItem);
        
        JsonTextEditor.ContextMenu = contextMenu;
    }

    private void UpdateFolding()
    {
        if (_foldingManager != null)
        {
            try
            {
                // Simple JSON folding - find braces and brackets
                var foldings = new List<NewFolding>();
                var text = JsonTextEditor.Text;
                var stack = new Stack<int>();

                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '{' || text[i] == '[')
                    {
                        stack.Push(i);
                    }
                    else if (text[i] == '}' || text[i] == ']')
                    {
                        if (stack.Count > 0)
                        {
                            var start = stack.Pop();
                            // Only add folding if there's meaningful content (more than just empty braces)
                            if (i - start > 1)
                            {
                                foldings.Add(new NewFolding(start, i + 1));
                            }
                        }
                    }
                }

                // Sort foldings by start offset (required by AvalonEdit)
                foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

                _foldingManager.UpdateFoldings(foldings, -1);
            }
            catch
            {
                // If folding fails, just ignore it silently
            }
        }
    }

    private void FormatJson_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(JsonTextEditor.Text))
            {
                MessageBox.Show("No content to format", "Format Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var jsonDocument = JsonDocument.Parse(JsonTextEditor.Text);
            var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            JsonTextEditor.Text = formattedJson;
            
            // Update folding after formatting
            UpdateFolding();
            
            // Auto-validate after formatting
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.ValidateJsonCommand.Execute(null);
            }
        }
        catch (JsonException ex)
        {
            var koreanMessage = JsonErrorHelper.GetKoreanErrorMessage(ex, JsonTextEditor.Text);
            var result = MessageBox.Show($"{koreanMessage}\n\n해당 라인으로 이동하시겠습니까?", "JSON 포맷 오류", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                GoToLine(GetLineNumberFromException(ex, JsonTextEditor.Text));
                HighlightError(GetLineNumberFromException(ex, JsonTextEditor.Text), (int)(ex.BytePositionInLine.GetValueOrDefault() + 1), ex.Message);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error formatting JSON: {ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
            ClearErrorHighlighting(); // Clear highlights on general error
        }
    }

    private void ValidateJson_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ValidateJsonCommand.Execute(null);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SaveFileCommand.Execute(null);
        }
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SaveAsFileCommand.Execute(null);
        }
    }

    public void GoToLine(int lineNumber)
    {
        ClearErrorHighlighting(); // Clear previous highlights

        if (lineNumber < 1) return;

        var line = JsonTextEditor.Document.GetLineByNumber(Math.Min(lineNumber, JsonTextEditor.Document.LineCount));
        JsonTextEditor.CaretOffset = line.Offset;
        JsonTextEditor.ScrollToLine(lineNumber);
        JsonTextEditor.Focus();
        
        // Select the entire line for better visibility
        JsonTextEditor.Select(line.Offset, line.Length);
    }

    private int GetLineNumberFromException(JsonException ex, string jsonContent)
    {
        // Use the improved method from JsonErrorHelper
        return JsonErrorHelper.GetLineNumberFromException(ex, jsonContent);
    }

    public void HighlightError(int lineNumber, int column, string message)
    {
        ClearErrorHighlighting();

        if (_textMarkerService != null && lineNumber >= 1 && lineNumber <= JsonTextEditor.Document.LineCount)
        {
            var line = JsonTextEditor.Document.GetLineByNumber(lineNumber);
            var startOffset = line.Offset + column - 1; // column is 1-based
            var length = Math.Min(5, line.Length - (column - 1)); // Highlight a small segment

            if (startOffset < line.EndOffset)
            {
                var marker = _textMarkerService.Create(startOffset, length);
                marker.BackgroundColor = Colors.Red;
                marker.ForegroundColor = Colors.White;
                marker.ToolTip = message;
                JsonTextEditor.TextArea.TextView.Redraw(); // Redraw to show the marker
            }
        }
    }

    public void ClearErrorHighlighting()
    {
        _textMarkerService?.RemoveAll(m => true);
        JsonTextEditor.TextArea.TextView.Redraw(); // Redraw to clear markers
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(JsonErrorOccurredMessage message)
    {
        // Switch to Code Editor tab first
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.SelectedTabIndex = 0; // Code Editor is index 0
        }

        // Navigate to the error line
        GoToLine(message.Value.Line);
        
        // Highlight the error
        HighlightError(message.Value.Line,
                       message.Value.Column,
                       message.Value.Message);
    }
}
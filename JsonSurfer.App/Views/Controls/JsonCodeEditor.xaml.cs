using System.Windows;
using System.Windows.Controls;
using JsonSurfer.App.ViewModels;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.Json;
using JsonSurfer.App.Helpers;

namespace JsonSurfer.App.Views.Controls;

public partial class JsonCodeEditor : UserControl
{
    private FoldingManager? _foldingManager;
    
    public JsonCodeEditor()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SetupContextMenu();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Setup folding
        if (_foldingManager == null)
        {
            _foldingManager = FoldingManager.Install(JsonTextEditor.TextArea);
        }
        
        // Find MainViewModel in the DataContext chain
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainViewModel viewModel)
        {
            // Bind JsonContent to AvalonEdit
            JsonTextEditor.Text = viewModel.JsonContent;

            // Two-way binding setup
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.JsonContent))
                {
                    if (JsonTextEditor.Text != viewModel.JsonContent)
                    {
                        JsonTextEditor.Text = viewModel.JsonContent;
                        UpdateFolding();
                    }
                }
            };

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
        }
        catch (JsonException ex)
        {
            var koreanMessage = JsonErrorHelper.GetKoreanErrorMessage(ex, JsonTextEditor.Text);
            var result = MessageBox.Show($"{koreanMessage}\n\n해당 라인으로 이동하시겠습니까?", "JSON 포맷 오류", 
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                GoToLine(GetLineNumberFromException(ex, JsonTextEditor.Text));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error formatting JSON: {ex.Message}", "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ValidateJson_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = Window.GetWindow(this);
        if (mainWindow?.DataContext is MainViewModel viewModel)
        {
            viewModel.ValidateJsonCommand.Execute(null);
        }
    }

    public void GoToLine(int lineNumber)
    {
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
        // Try to extract line number from exception
        var match = System.Text.RegularExpressions.Regex.Match(ex.Message, @"line (\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int line))
        {
            return line;
        }

        // If no line number in exception, try to find position-based line
        if (ex.BytePositionInLine.HasValue)
        {
            var position = ex.BytePositionInLine.Value;
            var lines = jsonContent.Split('\n');
            var currentPos = 0;
            
            for (int i = 0; i < lines.Length; i++)
            {
                currentPos += lines[i].Length + 1; // +1 for newline
                if (currentPos >= position)
                {
                    return i + 1;
                }
            }
        }

        return 1; // Default to line 1 if can't determine
    }
}
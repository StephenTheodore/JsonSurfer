using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;

namespace JsonSurfer.App.Helpers;

public interface ITextMarker
{
    int StartOffset { get; }
    int Length { get; }
    int EndOffset { get; }

    Color? BackgroundColor { get; set; }
    Color? ForegroundColor { get; set; }
    object ToolTip { get; set; }

    event System.EventHandler? Deleted;

    bool Contains(int offset);
    void Delete();
}
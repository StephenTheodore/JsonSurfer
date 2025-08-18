using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace JsonSurfer.App.Helpers;

/// <summary>
/// Handles the text markers for a TextDocument.
/// </summary>
public class TextMarkerService : IBackgroundRenderer
{
    private readonly TextDocument _document;
    internal readonly TextSegmentCollection<TextMarker> TextMarkers;

    public TextMarkerService(TextDocument document)
    {
        _document = document;
        TextMarkers = new TextSegmentCollection<TextMarker>(document);
    }

    public ITextMarker Create(int startOffset, int length)
    {
        TextMarker m = new TextMarker(this, startOffset, length);
        TextMarkers.Add(m);
        return m;
    }

    public void Remove(TextMarker marker)
    {
        TextMarkers.Remove(marker);
    }

    public void RemoveAll(System.Func<ITextMarker, bool> predicate)
    {
        foreach (TextMarker m in TextMarkers.Where(predicate).ToList())
        {
            Remove(m);
        }
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (TextMarkers == null || !textView.VisualLinesValid)
            return;

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;

        foreach (TextMarker marker in TextMarkers.Where(m => m.BackgroundColor.HasValue))
        {
            foreach (var line in visualLines)
            {
                var lineStart = line.FirstDocumentLine.Offset;
                var lineEnd = line.LastDocumentLine.EndOffset;
                
                // Check if marker intersects with this line
                if (marker.StartOffset <= lineEnd && marker.EndOffset >= lineStart)
                {
                    var segmentStart = Math.Max(marker.StartOffset, lineStart);
                    var segmentEnd = Math.Min(marker.EndOffset, lineEnd);
                    
                    if (segmentStart < segmentEnd)
                    {
                        var startVC = line.GetVisualColumn(segmentStart - lineStart);
                        var endVC = line.GetVisualColumn(segmentEnd - lineStart);
                        
                        var rects = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, line, startVC, endVC);
                        foreach (var rect in rects)
                        {
                            drawingContext.DrawRectangle(new SolidColorBrush(marker.BackgroundColor.Value), null, rect);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Default implementation of ITextMarker.
    /// </summary>
    public class TextMarker : TextSegment, ITextMarker
    {
        private readonly TextMarkerService _service;

        public TextMarker(TextMarkerService service, int startOffset, int length)
        {
            _service = service;
            StartOffset = startOffset;
            Length = length;
            ToolTip = string.Empty;
        }

        public Color? BackgroundColor { get; set; }
        public Color? ForegroundColor { get; set; }
        public object ToolTip { get; set; }

        public event System.EventHandler? Deleted;

        public bool Contains(int offset)
        {
            return offset >= StartOffset && offset < EndOffset;
        }

        public void Delete()
        {
            _service.Remove(this);
            Deleted?.Invoke(this, System.EventArgs.Empty);
        }
    }
}
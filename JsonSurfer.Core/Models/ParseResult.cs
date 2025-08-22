using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Models;

public class ParseResult
{
    public bool IsSuccess { get; set; }
    public JsonNode? RootNode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public long LineNumber { get; set; }
    public long ColumnNumber { get; set; }
}
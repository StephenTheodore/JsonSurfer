namespace JsonSurfer.Core.Models;

public class JsonFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public long FileSize { get; set; }
    public bool IsModified { get; set; }
    public string OriginalContent { get; set; } = string.Empty;
    public string CurrentContent { get; set; } = string.Empty;
}
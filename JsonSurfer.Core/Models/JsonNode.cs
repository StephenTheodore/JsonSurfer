namespace JsonSurfer.Core.Models;

public class JsonNode
{
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public JsonNodeType Type { get; set; }
    public List<JsonNode> Children { get; set; } = [];
    public JsonNode? Parent { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
}

public enum JsonNodeType
{
    Object,
    Array,
    Property,
    String,
    Number,
    Boolean,
    Null
}
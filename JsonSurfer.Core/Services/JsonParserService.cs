using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Services;

public class JsonParserService : IJsonParserService
{
    public ParseResult ParseToTree(string jsonContent)
    {
        try
        {
            var document = JsonDocument.Parse(jsonContent);
            var rootNode = ParseElement(document.RootElement, null, string.Empty);
            
            return new ParseResult
            {
                IsSuccess = true,
                RootNode = rootNode,
                ErrorMessage = string.Empty,
                LineNumber = 0,
                ColumnNumber = 0
            };
        }
        catch (JsonException ex)
        {
            return new ParseResult
            {
                IsSuccess = false,
                RootNode = null,
                ErrorMessage = ex.Message,
                LineNumber = ex.LineNumber ?? 0,
                ColumnNumber = ex.BytePositionInLine ?? 0
            };
        }
    }

    public string SerializeFromTree(JsonNode rootNode)
    {
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            {
                WriteNode(writer, rootNode);
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    private void WriteNode(Utf8JsonWriter writer, JsonNode node)
    {
        switch (node.Type)
        {
            case JsonNodeType.Object:
                writer.WriteStartObject();
                foreach (var child in node.Children)
                {
                    writer.WritePropertyName(child.Key);
                    WriteNode(writer, child);
                }
                writer.WriteEndObject();
                break;

            case JsonNodeType.Array:
                writer.WriteStartArray();
                foreach (var child in node.Children)
                {
                    WriteNode(writer, child);
                }
                writer.WriteEndArray();
                break;

            case JsonNodeType.String:
                writer.WriteStringValue(node.Value?.ToString());
                break;

            case JsonNodeType.Number:
                if (node.Value is int intValue)
                    writer.WriteNumberValue(intValue);
                else if (node.Value is long longValue)
                    writer.WriteNumberValue(longValue);
                else if (node.Value is double doubleValue)
                    writer.WriteNumberValue(doubleValue);
                else if (node.Value is decimal decimalValue)
                    writer.WriteNumberValue(decimalValue);
                else
                    writer.WriteStringValue(node.Value?.ToString()); // Fallback for unknown number types
                break;

            case JsonNodeType.Boolean:
                if (node.Value is bool boolValue)
                    writer.WriteBooleanValue(boolValue);
                else
                    writer.WriteStringValue(node.Value?.ToString()); // Fallback
                break;

            case JsonNodeType.Null:
                writer.WriteNullValue();
                break;

            default:
                // Handle unknown type or throw exception
                break;
        }
    }

    public ValidationResult ValidateJson(string jsonContent)
    {
        var result = new ValidationResult();
        
        try
        {
            JsonDocument.Parse(jsonContent);
            result.IsValid = true;
        }
        catch (JsonException ex)
        {
            result.IsValid = false;
            result.Errors.Add(new ValidationError
            {
                Message = ex.Message,
                Line = (int)ex.LineNumber.GetValueOrDefault(),
                Column = (int)ex.BytePositionInLine.GetValueOrDefault() + 1, // Convert 0-based byte position to 1-based column
                Type = ErrorType.SyntaxError
            });
        }

        return result;
    }

    public ValidationResult ValidateJsonWithAutoFix(string jsonContent)
    {
        var autoFixService = new JsonAutoFixService();
        var fixResult = autoFixService.AnalyzeAndFix(jsonContent, applyFixes: false);
        
        return new ValidationResult
        {
            IsValid = !fixResult.Errors.Any(),
            Errors = fixResult.Errors,
            Warnings = [] // Can add auto-fix suggestions as warnings later
        };
    }

    public string FormatJson(string jsonContent)
    {
        var autoFixService = new JsonAutoFixService();
        var fixResult = autoFixService.AnalyzeAndFix(jsonContent, applyFixes: true);
        
        if (!fixResult.Errors.Any())
        {
            // Successfully fixed, now format it nicely
            try
            {
                var jsonDoc = JsonDocument.Parse(fixResult.FixedContent);
                using var stream = new MemoryStream();
                using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions 
                { 
                    Indented = true, 
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
                });
                
                jsonDoc.WriteTo(writer);
                writer.Flush();
                
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
            catch
            {
                // If formatting fails, return the fixed content as-is
                return fixResult.FixedContent;
            }
        }
        
        return fixResult.FixedContent;
    }

    public async Task<JsonNode?> ParseFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ParseToTree(content).RootNode;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> SaveFileAsync(string filePath, JsonNode rootNode)
    {
        try
        {
            var jsonContent = SerializeFromTree(rootNode);
            await File.WriteAllTextAsync(filePath, jsonContent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private JsonNode ParseElement(JsonElement element, JsonNode? parent, string key)
    {
        var node = new JsonNode
        {
            Key = key,
            Parent = parent
        };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                node.Type = JsonNodeType.Object;
                foreach (var property in element.EnumerateObject())
                {
                    var childNode = ParseElement(property.Value, node, property.Name);
                    node.Children.Add(childNode);
                }
                break;

            case JsonValueKind.Array:
                node.Type = JsonNodeType.Array;
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var childNode = ParseElement(item, node, $"[{index}]");
                    node.Children.Add(childNode);
                    index++;
                }
                break;

            case JsonValueKind.String:
                node.Type = JsonNodeType.String;
                node.Value = element.GetString();
                break;

            case JsonValueKind.Number:
                node.Type = JsonNodeType.Number;
                node.Value = element.GetDecimal();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                node.Type = JsonNodeType.Boolean;
                node.Value = element.GetBoolean();
                break;

            case JsonValueKind.Null:
                node.Type = JsonNodeType.Null;
                node.Value = null;
                break;
        }

        return node;
    }
}
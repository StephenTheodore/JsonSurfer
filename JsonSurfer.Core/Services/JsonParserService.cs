using System.Text.Json;
using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Services;

public class JsonParserService : IJsonParserService
{
    public JsonNode? ParseToTree(string jsonContent)
    {
        try
        {
            var document = JsonDocument.Parse(jsonContent);
            return ParseElement(document.RootElement, null, string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string SerializeFromTree(JsonNode rootNode)
    {
        // TODO: Implement tree to JSON serialization
        throw new NotImplementedException();
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
                Type = ErrorType.SyntaxError
            });
        }

        return result;
    }

    public async Task<JsonNode?> ParseFileAsync(string filePath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            return ParseToTree(content);
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
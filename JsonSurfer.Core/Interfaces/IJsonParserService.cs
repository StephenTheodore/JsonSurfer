using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Interfaces;

public interface IJsonParserService
{
    JsonNode? ParseToTree(string jsonContent);
    string SerializeFromTree(JsonNode rootNode);
    ValidationResult ValidateJson(string jsonContent);
    Task<JsonNode?> ParseFileAsync(string filePath);
    Task<bool> SaveFileAsync(string filePath, JsonNode rootNode);
}
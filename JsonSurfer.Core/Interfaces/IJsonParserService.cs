using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Interfaces;

public interface IJsonParserService
{
    ParseResult ParseToTree(string jsonContent);
    string SerializeFromTree(JsonNode rootNode);
    ValidationResult ValidateJson(string jsonContent);
    ValidationResult ValidateJsonWithAutoFix(string jsonContent);
    string FormatJson(string jsonContent);
    Task<JsonNode?> ParseFileAsync(string filePath);
    Task<bool> SaveFileAsync(string filePath, JsonNode rootNode);
}
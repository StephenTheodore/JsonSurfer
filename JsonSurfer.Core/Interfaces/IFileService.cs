using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Interfaces;

public interface IFileService
{
    Task<string> ReadFileAsync(string filePath);
    Task<bool> WriteFileAsync(string filePath, string content);
    Task<JsonFileInfo> GetFileInfoAsync(string filePath);
    bool IsValidJsonFile(string filePath);
}
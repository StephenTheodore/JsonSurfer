using JsonSurfer.Core.Interfaces;
using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Services;

public class FileService : IFileService
{
    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            return await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read file: {filePath}", ex);
        }
    }

    public async Task<bool> WriteFileAsync(string filePath, string content)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<JsonFileInfo> GetFileInfoAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var content = await ReadFileAsync(filePath);

            return new JsonFileInfo
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
                IsModified = false,
                OriginalContent = content,
                CurrentContent = content
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get file info: {filePath}", ex);
        }
    }

    public bool IsValidJsonFile(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".json" || extension == ".info";
    }
}
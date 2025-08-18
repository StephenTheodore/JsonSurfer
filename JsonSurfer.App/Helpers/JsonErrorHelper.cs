using System.Text.Json;
using System.Text.RegularExpressions;

namespace JsonSurfer.App.Helpers;

public static class JsonErrorHelper
{
    public static string GetKoreanErrorMessage(JsonException ex, string jsonContent)
    {
        var message = ex.Message;
        var lineNumber = GetLineNumber(ex, jsonContent);
        var lineContent = GetLineContent(jsonContent, lineNumber);

        // Common JSON error patterns with Korean messages
        if (message.Contains("unexpected character") || message.Contains("Invalid character"))
        {
            return $"Line {lineNumber}: 잘못된 문자가 있습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 특수문자나 잘못 입력된 문자를 확인해주세요.";
        }
        
        if (message.Contains("unterminated string") || message.Contains("Unterminated string"))
        {
            return $"Line {lineNumber}: 문자열이 제대로 닫히지 않았습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 따옴표(\")가 누락되었는지 확인해주세요.";
        }
        
        if (message.Contains("Expected") && message.Contains("comma"))
        {
            return $"Line {lineNumber}: 콤마(,)가 필요합니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 속성 사이에 콤마를 추가해주세요.";
        }
        
        if (message.Contains("trailing comma") || message.Contains("Trailing comma"))
        {
            return $"Line {lineNumber}: 마지막에 불필요한 콤마가 있습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 마지막 속성 뒤의 콤마를 제거해주세요.";
        }
        
        if (message.Contains("Expected") && (message.Contains("'}'") || message.Contains("']'")))
        {
            return $"Line {lineNumber}: 중괄호 또는 대괄호가 닫히지 않았습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 열린 괄호에 맞는 닫는 괄호를 추가해주세요.";
        }
        
        if (message.Contains("duplicate key") || message.Contains("Duplicate"))
        {
            return $"Line {lineNumber}: 중복된 속성명이 있습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 같은 속성명을 두 번 사용할 수 없습니다. 하나를 제거하거나 이름을 변경해주세요.";
        }
        
        if (message.Contains("Expected") && message.Contains("':'"))
        {
            return $"Line {lineNumber}: 콜론(:)이 누락되었습니다.\n" +
                   $"해당 줄: {lineContent?.Trim()}\n" +
                   $"해결방법: 속성명과 값 사이에 콜론(:)을 추가해주세요.";
        }

        // Default case with line info
        return $"Line {lineNumber}: JSON 구문 오류가 발생했습니다.\n" +
               $"해당 줄: {lineContent?.Trim()}\n" +
               $"상세오류: {message}";
    }

    public static int GetLineNumberFromException(JsonException ex, string jsonContent)
    {
        return GetLineNumber(ex, jsonContent);
    }

    private static int GetLineNumber(JsonException ex, string jsonContent)
    {
        // Try to extract line number from exception message first
        var lineMatch = Regex.Match(ex.Message, @"line (\d+)", RegexOptions.IgnoreCase);
        if (lineMatch.Success && int.TryParse(lineMatch.Groups[1].Value, out int lineFromMessage))
        {
            return lineFromMessage;
        }

        // Try position-based calculation using LinePosition property if available
        if (ex.LineNumber.HasValue)
        {
            return (int)ex.LineNumber.Value + 1; // LineNumber is 0-based
        }

        // Fallback to BytePositionInLine calculation with improved accuracy
        if (ex.BytePositionInLine.HasValue)
        {
            return CalculateLineFromBytePosition(ex.BytePositionInLine.Value, jsonContent);
        }

        // Last resort: try to parse position from the entire exception message
        var posMatch = Regex.Match(ex.Message, @"position (\d+)", RegexOptions.IgnoreCase);
        if (posMatch.Success && int.TryParse(posMatch.Groups[1].Value, out int position))
        {
            return CalculateLineFromBytePosition(position, jsonContent);
        }

        return 1; // Default to line 1 if can't determine
    }

    private static int CalculateLineFromBytePosition(long bytePosition, string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent) || bytePosition < 0)
            return 1;

        // Convert string to bytes using UTF-8 encoding (same as System.Text.Json)
        var bytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
        
        if (bytePosition >= bytes.Length)
            return jsonContent.Split('\n').Length; // Return last line if position exceeds content

        // Count newlines up to the byte position
        int lineNumber = 1;
        for (int i = 0; i < Math.Min(bytePosition, bytes.Length); i++)
        {
            if (bytes[i] == 0x0A) // '\n' in UTF-8
            {
                lineNumber++;
            }
        }

        return lineNumber;
    }

    private static string? GetLineContent(string jsonContent, int lineNumber)
    {
        var lines = jsonContent.Split('\n');
        if (lineNumber > 0 && lineNumber <= lines.Length)
        {
            return lines[lineNumber - 1];
        }
        return null;
    }
}
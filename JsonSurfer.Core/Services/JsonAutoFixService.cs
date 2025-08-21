using JsonSurfer.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace JsonSurfer.Core.Services;

public class JsonAutoFixService
{
    private readonly List<ValidationError> _foundErrors = [];

    public JsonAutoFixResult AnalyzeAndFix(string jsonContent, bool applyFixes = false)
    {
        _foundErrors.Clear();
        
        var result = new JsonAutoFixResult
        {
            OriginalContent = jsonContent,
            FixedContent = jsonContent,
            Errors = [],
            FixesApplied = []
        };

        // Recursive parsing with error accumulation
        var currentContent = jsonContent;
        var maxIterations = 10; // Prevent infinite loops
        var iteration = 0;

        while (iteration < maxIterations)
        {
            var parseResult = TryParseWithErrorCapture(currentContent);
            
            if (parseResult.IsValid)
            {
                // Successfully parsed - we're done
                break;
            }

            // Add error to our collection
            _foundErrors.AddRange(parseResult.Errors);

            if (!applyFixes)
            {
                // Just collecting errors, don't actually fix
                // Try to find more errors by attempting common fixes
                currentContent = ApplyCommonFixes(currentContent, out var fixes);
                result.FixesApplied.AddRange(fixes);
            }
            else
            {
                // Apply fixes and continue
                var fixedContent = ApplyCommonFixes(currentContent, out var fixes);
                if (fixedContent == currentContent)
                {
                    // No more fixes possible
                    break;
                }
                
                currentContent = fixedContent;
                result.FixesApplied.AddRange(fixes);
            }

            iteration++;
        }

        result.FixedContent = applyFixes ? currentContent : jsonContent;
        result.Errors = _foundErrors.ToList();

        return result;
    }

    private ValidationResult TryParseWithErrorCapture(string jsonContent)
    {
        var result = new ValidationResult();
        var errors = new List<ValidationError>();
        
        // Manual parsing to collect ALL errors, not just the first one
        errors.AddRange(FindAllSyntaxErrors(jsonContent));
        
        // Always try JSON parsing with different options to catch more errors
        var parsingErrors = TryParseWithDifferentOptions(jsonContent);
        errors.AddRange(parsingErrors);
        
        // Remove duplicates (same line, column, and type)
        errors = errors
            .GroupBy(e => new { e.Line, e.Column, e.Type, e.Message })
            .Select(g => g.First())
            .OrderBy(e => e.Line)
            .ThenBy(e => e.Column)
            .ToList();

        if (!errors.Any())
        {
            result.IsValid = true;
        }
        
        result.IsValid = !errors.Any();
        result.Errors = errors;
        return result;
    }

    private List<ValidationError> FindAllSyntaxErrors(string jsonContent)
    {
        var errors = new List<ValidationError>();
        var lines = jsonContent.Split('\n');
        
        // Only run manual detection if JSON parsing completely fails
        // This reduces duplicate detection
        try
        {
            JsonDocument.Parse(jsonContent);
            // If parsing succeeds, no need for manual detection
            return errors;
        }
        catch
        {
            // Continue with manual detection
        }
        
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;
            
            // Check for unescaped control characters (like 0x0D)
            var controlCharMatches = Regex.Matches(line, @"[^\x20-\x7E\t\r\n]");
            foreach (Match match in controlCharMatches)
            {
                var charCode = (int)line[match.Index];
                errors.Add(new ValidationError
                {
                    Message = $"Invalid control character '0x{charCode:X}' in JSON string",
                    Line = lineNumber,
                    Column = match.Index + 1,
                    Type = ErrorType.InvalidValue
                });
            }
            
            // Check for single quotes (only if not inside already-quoted strings)
            if (!Regex.IsMatch(line, @"""[^""]*'[^""]*"""))
            {
                var singleQuoteMatches = Regex.Matches(line, @"'([^']*)'");
                foreach (Match match in singleQuoteMatches)
                {
                    errors.Add(new ValidationError
                    {
                        Message = "Single quotes should be double quotes",
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Type = ErrorType.SyntaxError
                    });
                }
            }
            
            // Check for unquoted property names (improved detection)
            var unquotedPropMatches = Regex.Matches(line, @"(\s*)([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:");
            foreach (Match match in unquotedPropMatches)
            {
                // Check if it's not already inside quotes
                var beforeMatch = line.Substring(0, match.Index);
                var quoteCount = beforeMatch.Count(c => c == '"');
                var isInsideString = quoteCount % 2 == 1;
                
                if (!isInsideString)
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"Property name '{match.Groups[2].Value}' should be quoted",
                        Line = lineNumber,
                        Column = match.Index + match.Groups[1].Length + 1,
                        Type = ErrorType.SyntaxError
                    });
                }
            }
            
            // Check for wrong boolean/null casing (outside of strings)
            var wrongCaseMatches = Regex.Matches(line, @"\b(True|False|Null)\b");
            foreach (Match match in wrongCaseMatches)
            {
                // Check if it's not inside quotes
                var beforeMatch = line.Substring(0, match.Index);
                var quoteCount = beforeMatch.Count(c => c == '"');
                var isInsideString = quoteCount % 2 == 1;
                
                if (!isInsideString)
                {
                    errors.Add(new ValidationError
                    {
                        Message = $"'{match.Value}' should be lowercase",
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Type = ErrorType.SyntaxError
                    });
                }
            }
        }
        
        return errors;
    }

    private List<ValidationError> TryParseWithDifferentOptions(string jsonContent)
    {
        var errors = new List<ValidationError>();
        
        // Standard strict mode - will catch first trailing comma
        TryParseWithOptions(jsonContent, new JsonDocumentOptions(), errors);
        
        // Find ALL trailing commas by testing each potential trailing comma fix
        var allTrailingCommaErrors = FindAllTrailingCommas(jsonContent);
        errors.AddRange(allTrailingCommaErrors);
        
        // Allow trailing commas to find other errors
        TryParseWithOptions(jsonContent, new JsonDocumentOptions 
        { 
            AllowTrailingCommas = true 
        }, errors);
        
        // Allow comments to find other errors  
        TryParseWithOptions(jsonContent, new JsonDocumentOptions 
        { 
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }, errors);
        
        return errors;
    }

    private List<ValidationError> FindAllTrailingCommas(string jsonContent)
    {
        var errors = new List<ValidationError>();
        var lines = jsonContent.Split('\n');
        
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];
            var lineNumber = lineIndex + 1;
            
            // More comprehensive trailing comma detection
            var trailingCommaMatches = Regex.Matches(line, @",(\s*[}\]])");
            foreach (Match match in trailingCommaMatches)
            {
                // Simple check: count unescaped quotes before the comma
                var beforeMatch = line.Substring(0, match.Index);
                var unescapedQuoteCount = 0;
                var escapeNext = false;
                
                foreach (char c in beforeMatch)
                {
                    if (escapeNext)
                    {
                        escapeNext = false;
                        continue;
                    }
                    if (c == '\\')
                    {
                        escapeNext = true;
                        continue;
                    }
                    if (c == '"')
                    {
                        unescapedQuoteCount++;
                    }
                }
                
                var isInsideString = unescapedQuoteCount % 2 == 1;
                
                if (!isInsideString)
                {
                    errors.Add(new ValidationError
                    {
                        Message = "Trailing comma detected",
                        Line = lineNumber,
                        Column = match.Index + 1,
                        Type = ErrorType.SyntaxError
                    });
                }
            }
            
            // Also check for trailing commas at end of arrays/objects that span multiple lines
            // Look for commas followed by only whitespace and then closing bracket on next lines
            if (line.TrimEnd().EndsWith(','))
            {
                // Check if the next non-empty lines contain only closing brackets
                var nextNonEmptyLineIndex = lineIndex + 1;
                while (nextNonEmptyLineIndex < lines.Length && string.IsNullOrWhiteSpace(lines[nextNonEmptyLineIndex]))
                {
                    nextNonEmptyLineIndex++;
                }
                
                if (nextNonEmptyLineIndex < lines.Length)
                {
                    var nextLine = lines[nextNonEmptyLineIndex].Trim();
                    if (nextLine.StartsWith('}') || nextLine.StartsWith(']'))
                    {
                        var commaIndex = line.LastIndexOf(',');
                        errors.Add(new ValidationError
                        {
                            Message = "Trailing comma before closing bracket",
                            Line = lineNumber,
                            Column = commaIndex + 1,
                            Type = ErrorType.SyntaxError
                        });
                    }
                }
            }
        }
        
        return errors;
    }
    
    private void TryParseWithOptions(string jsonContent, JsonDocumentOptions options, List<ValidationError> errors)
    {
        try
        {
            JsonDocument.Parse(jsonContent, options);
        }
        catch (JsonException ex)
        {
            errors.Add(new ValidationError
            {
                Message = ex.Message,
                Line = (int)ex.LineNumber.GetValueOrDefault(),
                Column = (int)ex.BytePositionInLine.GetValueOrDefault() + 1,
                Type = ErrorType.SyntaxError
            });
        }
    }

    private string ApplyCommonFixes(string jsonContent, out List<string> appliedFixes)
    {
        appliedFixes = [];
        var fixedContent = jsonContent;

        // Fix 1: Remove trailing commas
        var trailingCommaPattern = @",(\s*[}\]])";
        if (Regex.IsMatch(fixedContent, trailingCommaPattern))
        {
            fixedContent = Regex.Replace(fixedContent, trailingCommaPattern, "$1");
            appliedFixes.Add("Removed trailing commas");
        }

        // Fix 2: Replace single quotes with double quotes (for property names and string values)
        var singleQuotePattern = @"'([^']*)'";
        if (Regex.IsMatch(fixedContent, singleQuotePattern))
        {
            fixedContent = Regex.Replace(fixedContent, singleQuotePattern, "\"$1\"");
            appliedFixes.Add("Converted single quotes to double quotes");
        }

        // Fix 3: Add missing quotes around property names
        var unquotedPropertyPattern = @"(\s*)([a-zA-Z_$][a-zA-Z0-9_$]*)\s*:";
        if (Regex.IsMatch(fixedContent, unquotedPropertyPattern))
        {
            fixedContent = Regex.Replace(fixedContent, unquotedPropertyPattern, "$1\"$2\":");
            appliedFixes.Add("Added quotes around property names");
        }

        // Fix 4: Fix common boolean/null value cases
        fixedContent = Regex.Replace(fixedContent, @"\bTrue\b", "true");
        fixedContent = Regex.Replace(fixedContent, @"\bFalse\b", "false");
        fixedContent = Regex.Replace(fixedContent, @"\bNull\b", "null");
        if (Regex.IsMatch(jsonContent, @"\b(True|False|Null)\b"))
        {
            appliedFixes.Add("Fixed boolean/null casing");
        }

        return fixedContent;
    }
}

public class JsonAutoFixResult
{
    public string OriginalContent { get; set; } = string.Empty;
    public string FixedContent { get; set; } = string.Empty;
    public List<ValidationError> Errors { get; set; } = [];
    public List<string> FixesApplied { get; set; } = [];
    public bool HasFixableErrors => FixesApplied.Any();
}
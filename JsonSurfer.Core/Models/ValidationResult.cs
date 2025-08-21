namespace JsonSurfer.Core.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = [];
    public List<ValidationWarning> Warnings { get; set; } = [];
}

public class ValidationError
{
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public ErrorType Type { get; set; }
}

public class ValidationWarning
{
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public WarningType Type { get; set; }
}

public enum ErrorType
{
    SyntaxError,
    InvalidFormat,
    MissingProperty,
    InvalidValue
}

public enum WarningType
{
    PossibleTypo,
    InconsistentStructure,
    UnusualValue,
    DuplicateKey
}

public class ProblemItem
{
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
using JsonSurfer.Core.Models;

namespace JsonSurfer.Core.Interfaces;

public interface IValidationService
{
    ValidationResult ValidateStructure(JsonNode rootNode);
    ValidationResult CheckConsistency(JsonNode rootNode);
    List<ValidationWarning> DetectTypos(JsonNode rootNode);
    List<ValidationWarning> FindStructuralInconsistencies(JsonNode rootNode);
}
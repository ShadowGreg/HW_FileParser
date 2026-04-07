using System.ComponentModel.DataAnnotations;

namespace HW_FileParser.Entities.DTO;

public sealed record UrlsRequest : IValidatableObject
{
    public required IReadOnlyList<string> Urls { get; init; }
    public string? RequestId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (Urls is null || Urls.Count == 0) {
            yield return new ValidationResult("Список URL не должен быть пустым.", [nameof(Urls)]);
            yield break;
        }

        if (Urls.Count > 100) {
            yield return new ValidationResult("Не более 100 URL за один запрос.", [nameof(Urls)]);
        }
    }
}

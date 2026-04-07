using System.ComponentModel.DataAnnotations;
using HW_FileParser.Options;
using Microsoft.Extensions.Options;

namespace HW_FileParser.Entities.DTO;
public sealed record UrlsRequest: IValidatableObject
{
    public required IReadOnlyList<string> Urls { get; init; }
    public string? RequestId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (Urls.Count == 0) {
            yield return new ValidationResult("Список URL не должен быть пустым.", [nameof(Urls)]);
            yield break;
        }

        int maxConnections = validationContext.GetRequiredService<IOptions<DownloaderServiceOptions>>().Value
                                              .MaxConnections;

        if (Urls.Count > maxConnections) {
            yield return new ValidationResult(
                $"Не более {maxConnections} URL за один запрос.",
                [nameof(Urls)]);
        }
    }
}
using System.ComponentModel.DataAnnotations;
using HW_FileParser.Options;
using Microsoft.Extensions.Options;

namespace HW_FileParser.Models;

public sealed record UrlsRequest: IValidatableObject
{
    public required IReadOnlyList<string> Urls { get; init; }
    public string? RequestId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) {
        if (Urls.Count == 0) {
            yield return new ValidationResult("Список URL не должен быть пустым.", [nameof(Urls)]);
            yield break;
        }

        int maxUrls = validationContext.GetRequiredService<IOptions<DownloaderServiceOptions>>().Value
                                       .MaxUrlsPerRequest;

        if (Urls.Count > maxUrls) {
            yield return new ValidationResult(
                $"Не более {maxUrls} URL за один запрос.",
                [nameof(Urls)]);
        }
    }
}

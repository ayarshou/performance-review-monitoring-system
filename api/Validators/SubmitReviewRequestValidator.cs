using FluentValidation;
using PerformanceReviewApi.DTOs;

namespace PerformanceReviewApi.Validators;

public class SubmitReviewRequestValidator : AbstractValidator<SubmitReviewRequest>
{
    public SubmitReviewRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .WithMessage("Notes must not exceed 2000 characters.");
    }
}

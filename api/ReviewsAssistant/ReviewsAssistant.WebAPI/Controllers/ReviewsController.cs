using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using ReviewsAssistant.Application.Reviews;

namespace ReviewsAssistant.WebAPI.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewInput input, CancellationToken cancellationToken)
    {
        var review = await reviewService.CreateAsync(new CreateReviewRequest(input.AuthorName, input.Email, input.Text), cancellationToken);
        return CreatedAtAction(nameof(GetSubmissionStatus), new { id = review.Id }, review);
    }

    [HttpGet("{id:guid}/submission-status")]
    public async Task<ActionResult<SubmissionStatusDto>> GetSubmissionStatus(Guid id, CancellationToken cancellationToken)
    {
        var status = await reviewService.GetSubmissionStatusAsync(id, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }
}

public sealed class CreateReviewInput
{
    [Required, StringLength(150)] public string AuthorName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(320)] public string Email { get; init; } = string.Empty;
    [Required, StringLength(4000, MinimumLength = 3)] public string Text { get; init; } = string.Empty;
}

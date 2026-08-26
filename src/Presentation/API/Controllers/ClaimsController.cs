using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SanlamClaims.Application.Claims.DTOs;
using SanlamClaims.Application.Claims.Mapping;
using SanlamClaims.Application.Claims.Services.Interfaces;
using SanlamClaims.Domain.Common;
using SanlamClaims.Domain.Enums;

namespace SanlamClaims.API.Controllers;

[ApiController]
[Route("api/v1/claims")]
[Authorize]
public class ClaimsController : ControllerBase
{
    private readonly IClaimSubmissionService _submissionService;
    private readonly IClaimAssessmentService _assessmentService;
    private readonly IClaimPaymentService _paymentService;
    private readonly IClaimQueryService _queryService;

    public ClaimsController(
        IClaimSubmissionService submissionService,
        IClaimAssessmentService assessmentService,
        IClaimPaymentService paymentService,
        IClaimQueryService queryService)
    {
        _submissionService = submissionService;
        _assessmentService = assessmentService;
        _paymentService = paymentService;
        _queryService = queryService;
    }

    /// <summary>Verifies the claimant and policy, registers the claim, and queues it for review.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("public")]
    public async Task<ActionResult<ClaimResponse>> Submit([FromBody] SubmitClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _submissionService.SubmitAsync(request, cancellationToken);
        return Ok(ClaimMapper.ToResponse(claim));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClaimResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var claim = await _queryService.GetByIdAsync(id, cancellationToken);
        return Ok(ClaimMapper.ToResponse(claim));
    }

    /// <summary>Lists claims, filterable and paged, sorted most-urgent first.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ClaimSummaryResponse>>> Get(
        [FromQuery] ClaimStatus? status,
        [FromQuery] ClaimType? claimType,
        [FromQuery] bool? slaBreached,
        [FromQuery] bool? possibleDuplicatesOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetAsync(status, claimType, slaBreached, possibleDuplicatesOnly, page, pageSize, cancellationToken);
        var summaries = result.Items.Select(ClaimMapper.ToSummary).ToList();
        return Ok(new PagedResult<ClaimSummaryResponse>(summaries, result.Page, result.PageSize, result.TotalCount));
    }

    /// <summary>Submits a Claims Analyst's approve/reject decision on a claim pending review.</summary>
    [HttpPost("{id:guid}/assessment")]
    public async Task<ActionResult<ClaimResponse>> SubmitAssessment(Guid id, [FromBody] AssessClaimRequest request, CancellationToken cancellationToken)
    {
        var claim = await _assessmentService.AssessAsync(id, request, cancellationToken);
        return Ok(ClaimMapper.ToResponse(claim));
    }

    /// <summary>Requests or retries payment for an approved claim — processed asynchronously.</summary>
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<ClaimResponse>> RequestPayment(Guid id, CancellationToken cancellationToken)
    {
        var claim = await _paymentService.RequestPaymentAsync(id, cancellationToken);
        return Ok(ClaimMapper.ToResponse(claim));
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<ClaimHistoryEntryResponse>>> GetHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _queryService.GetHistoryAsync(id, cancellationToken);
        return Ok(history.Select(ClaimMapper.ToHistoryEntry).ToList());
    }
}

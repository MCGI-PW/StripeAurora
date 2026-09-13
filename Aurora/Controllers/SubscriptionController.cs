using System;

using Microsoft.AspNetCore.Mvc;
using AuroraPet.Payments.Application.Interfaces;

namespace AuroraPet.Payments.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public record CreateSubscriptionRequest(string FirebaseUid, string Email, string PlanTier);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        var (subscriptionId, clientSecret) = await _subscriptionService
            .CreateSubscriptionAsync(request.FirebaseUid, request.Email, request.PlanTier, ct);

        return Ok(new { subscriptionId, clientSecret });
    }
}

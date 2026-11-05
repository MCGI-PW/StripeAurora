using AuroraPet.Payments.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPet.Payments.Controllers;

/// <summary>
/// Checkout de assinaturas na Stripe. O CRUD das tabelas fica em /api/plans e /api/subscriptions;
/// aqui ficam apenas as operacoes que conversam com o provedor de pagamentos.
/// </summary>
[ApiController]
[Authorize]
[Route("api/checkout")]
[Produces("application/json")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    public record CreateSubscriptionRequest(string FirebaseUid, string Email, string PlanTier);

    /// <summary>Cria o customer e a subscription na Stripe e devolve o client secret do pagamento.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken ct)
    {
        try
        {
            var (subscriptionId, clientSecret) = await _subscriptionService
                .CreateSubscriptionAsync(request.FirebaseUid, request.Email, request.PlanTier, ct);

            return Ok(new { subscriptionId, clientSecret });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Plano invalido", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Cancela uma assinatura diretamente na Stripe.</summary>
    [HttpDelete("{stripeSubscriptionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(string stripeSubscriptionId, CancellationToken ct)
    {
        await _subscriptionService.CancelSubscriptionAsync(stripeSubscriptionId, ct);
        return NoContent();
    }
}

using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuroraPet.Payments.Controllers;

/// <summary>
/// CRUD completo da tabela TB_CLINIC_SUBSCRIPTION, que associa TB_CLINIC a TB_SUBSCRIPTION_PLAN.
/// </summary>
[ApiController]
[Authorize]
[Route("api/subscriptions")]
[Produces("application/json")]
public class ClinicSubscriptionsController : ControllerBase
{
    private readonly IClinicSubscriptionService _service;

    public ClinicSubscriptionsController(IClinicSubscriptionService service) => _service = service;

    /// <summary>Consulta todas as assinaturas cadastradas.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClinicSubscriptionResponse>>> GetAll(CancellationToken ct) =>
        Ok(await _service.GetAllAsync(ct));

    /// <summary>Consulta uma assinatura pelo identificador.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicSubscriptionResponse>> GetById(Guid id, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.GetByIdAsync(id, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Assinatura nao encontrada", Detail = ex.Message, Status = 404 });
        }
    }

    /// <summary>Inclui uma assinatura vinculando uma clinica a um plano.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicSubscriptionResponse>> Create([FromBody] ClinicSubscriptionRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Recurso nao encontrado", Detail = ex.Message, Status = 404 });
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "Contratacao bloqueada", Detail = ex.Message, Status = 409 });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ProblemDetails { Title = "Assinatura invalida", Detail = ex.Message, Status = 400 });
        }
    }

    /// <summary>Altera o status, a vigencia e/ou o plano de uma assinatura.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClinicSubscriptionResponse>> Update(Guid id, [FromBody] ClinicSubscriptionUpdateRequest request, CancellationToken ct)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, ct));
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Recurso nao encontrado", Detail = ex.Message, Status = 404 });
        }
        catch (BusinessRuleException ex)
        {
            return Conflict(new ProblemDetails { Title = "Alteracao bloqueada", Detail = ex.Message, Status = 409 });
        }
    }

    /// <summary>Exclui uma assinatura.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (ResourceNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = "Assinatura nao encontrada", Detail = ex.Message, Status = 404 });
        }
    }
}

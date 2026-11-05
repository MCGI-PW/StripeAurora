using AuroraPet.Payments.Application.Dtos;
using AuroraPet.Payments.Application.Interfaces;
using AuroraPet.Payments.Domain.Entities;

namespace AuroraPet.Payments.Application.Services;

/// <summary>Casos de uso de CRUD sobre as clinicas.</summary>
public interface IClinicService
{
    Task<IReadOnlyList<ClinicResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ClinicResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ClinicResponse> CreateAsync(ClinicRequest request, CancellationToken ct = default);
    Task<ClinicResponse> UpdateAsync(Guid id, ClinicUpdateRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public class ClinicService : IClinicService
{
    private readonly IClinicRepository _clinics;
    private readonly IClinicSubscriptionRepository _subscriptions;
    private readonly ILogger<ClinicService> _logger;

    public ClinicService(
        IClinicRepository clinics,
        IClinicSubscriptionRepository subscriptions,
        ILogger<ClinicService> logger)
    {
        _clinics = clinics;
        _subscriptions = subscriptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ClinicResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var clinics = await _clinics.ListAsync(ct);
        return clinics.Select(ClinicResponse.FromEntity).ToList();
    }

    public async Task<ClinicResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Clinica {id} nao encontrada.");

        return ClinicResponse.FromEntity(clinic);
    }

    public async Task<ClinicResponse> CreateAsync(ClinicRequest request, CancellationToken ct = default)
    {
        var clinic = new Clinic(request.Name, request.Cnpj, request.Email, request.FirebaseUid);

        // O indice unico do Oracle ja barra, mas validar aqui devolve 409 em vez de 500.
        if (await _clinics.GetByCnpjAsync(clinic.Cnpj, ct) is not null)
            throw new BusinessRuleException($"Ja existe uma clinica cadastrada com o CNPJ {clinic.Cnpj}.");

        await _clinics.AddAsync(clinic, ct);
        await _clinics.SaveChangesAsync(ct);

        _logger.LogInformation("Clinica {ClinicId} ({Name}) cadastrada", clinic.Id, clinic.Name);
        return ClinicResponse.FromEntity(clinic);
    }

    public async Task<ClinicResponse> UpdateAsync(Guid id, ClinicUpdateRequest request, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Clinica {id} nao encontrada.");

        clinic.Update(request.Name, request.Cnpj, request.Email);

        if (request.Active) clinic.Activate();
        else clinic.Deactivate();

        await _clinics.SaveChangesAsync(ct);

        _logger.LogInformation("Clinica {ClinicId} atualizada para {Name}", clinic.Id, clinic.Name);
        return ClinicResponse.FromEntity(clinic);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var clinic = await _clinics.GetByIdAsync(id, ct)
            ?? throw new ResourceNotFoundException($"Clinica {id} nao encontrada.");

        var assinaturas = await _subscriptions.ListByClinicAsync(id, ct);
        if (assinaturas.Count > 0)
            throw new BusinessRuleException($"A clinica {id} possui {assinaturas.Count} assinatura(s) e nao pode ser excluida.");

        await _clinics.RemoveAsync(clinic, ct);
        await _clinics.SaveChangesAsync(ct);

        _logger.LogWarning("Clinica {ClinicId} excluida", id);
    }
}

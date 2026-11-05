using System;
using System.Text.RegularExpressions;

namespace AuroraPet.Payments.Domain.Entities;

/// <summary>
/// Clinica veterinaria cadastrada no Aurora. E a assinante dos planos: e a clinica que
/// contrata, nao o profissional nem o tutor do animal.
/// </summary>
public class Clinic
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string FirebaseUid { get; private set; } = string.Empty;
    public bool Active { get; private set; }

    private Clinic() { } // EF Core

    public Clinic(string name, string cnpj, string email, string firebaseUid)
    {
        Validar(name, cnpj, email, firebaseUid);

        Id = Guid.NewGuid();
        Name = name;
        Cnpj = SomenteDigitos(cnpj);
        Email = email;
        FirebaseUid = firebaseUid;
        Active = true;
    }

    public void Update(string name, string cnpj, string email)
    {
        Validar(name, cnpj, email, FirebaseUid);

        Name = name;
        Cnpj = SomenteDigitos(cnpj);
        Email = email;
    }

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;

    private static void Validar(string name, string cnpj, string email, string firebaseUid)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("O nome da clinica e obrigatorio.", nameof(name));

        if (SomenteDigitos(cnpj).Length != 14)
            throw new ArgumentException("O CNPJ da clinica deve conter 14 digitos.", nameof(cnpj));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new ArgumentException("E-mail da clinica invalido.", nameof(email));

        if (string.IsNullOrWhiteSpace(firebaseUid))
            throw new ArgumentException("O uid da clinica no Firebase e obrigatorio.", nameof(firebaseUid));
    }

    private static string SomenteDigitos(string valor) =>
        string.IsNullOrEmpty(valor) ? string.Empty : Regex.Replace(valor, @"\D", "");
}

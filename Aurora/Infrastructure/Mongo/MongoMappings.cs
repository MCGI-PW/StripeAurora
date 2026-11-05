using System;
using AuroraPet.Payments.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>
/// Mapeamento das entidades de dominio para documentos BSON. Fica na infraestrutura para
/// que o dominio nao dependa de atributos do driver ([BsonId], [BsonElement] etc.).
/// </summary>
public static class MongoMappings
{
    private static readonly object Sync = new();
    private static bool _registered;

    /// <summary>Registra serializers, convencoes e class maps. Idempotente: pode ser chamado varias vezes.</summary>
    public static void Register()
    {
        lock (Sync)
        {
            if (_registered) return;

            // GUID no formato padrao (UUID subtipo 4) e decimal como Decimal128, sem perda de precisao no preco.
            TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            TryRegisterSerializer(new DecimalSerializer(BsonType.Decimal128));

            var conventions = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register(
                "aurora-conventions",
                conventions,
                type => type.Namespace?.StartsWith("AuroraPet", StringComparison.Ordinal) == true);

            RegisterEntity<Clinic>();
            RegisterEntity<SubscriptionPlan>();
            RegisterEntity<ClinicSubscription>();
            RegisterEntity<User>();

            _registered = true;
        }
    }

    /// <summary>
    /// As entidades tem construtor sem parametros privado e setters privados: o driver
    /// cria a instancia por esse construtor e preenche as propriedades por reflexao.
    /// A propriedade Id vira o campo _id por convencao.
    /// </summary>
    private static void RegisterEntity<T>() where T : class
    {
        if (BsonClassMap.IsClassMapRegistered(typeof(T))) return;

        BsonClassMap.RegisterClassMap<T>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            cm.SetCreator(() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!);
        });
    }

    private static void TryRegisterSerializer<T>(IBsonSerializer<T> serializer)
    {
        try
        {
            BsonSerializer.RegisterSerializer(serializer);
        }
        catch (BsonSerializationException)
        {
            // Ja registrado (ex.: varios WebApplicationFactory no mesmo processo de testes).
        }
    }
}

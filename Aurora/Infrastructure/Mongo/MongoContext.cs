using AuroraPet.Payments.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>Ponto unico de acesso ao banco e as colecoes do Aurora no MongoDB.</summary>
public class MongoContext
{
    public const string ClinicsCollection = "clinics";
    public const string PlansCollection = "subscription_plans";
    public const string SubscriptionsCollection = "clinic_subscriptions";
    public const string UsersCollection = "users";

    public MongoContext(IMongoClient client, IOptions<MongoDbOptions> options)
    {
        MongoMappings.Register();
        Database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoDatabase Database { get; }

    public IMongoCollection<Clinic> Clinics => Database.GetCollection<Clinic>(ClinicsCollection);

    public IMongoCollection<SubscriptionPlan> SubscriptionPlans => Database.GetCollection<SubscriptionPlan>(PlansCollection);

    public IMongoCollection<ClinicSubscription> ClinicSubscriptions => Database.GetCollection<ClinicSubscription>(SubscriptionsCollection);

    public IMongoCollection<User> Users => Database.GetCollection<User>(UsersCollection);
}

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>Configuracao do MongoDB (secao "MongoDb" do appsettings / variaveis MongoDb__*).</summary>
public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    /// <summary>Ex.: mongodb://localhost:27017 ou mongodb+srv://usuario:senha@cluster.mongodb.net</summary>
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";

    public string DatabaseName { get; set; } = "aurora";

    /// <summary>Quanto tempo o driver espera para achar um servidor antes de falhar.</summary>
    public int ServerSelectionTimeoutSeconds { get; set; } = 5;

    /// <summary>Insere a carga inicial (2 clinicas, 2 planos, 2 assinaturas) quando as colecoes estao vazias.</summary>
    public bool SeedData { get; set; } = true;
}

/// <summary>Qual banco atende os repositorios de clinicas, planos e assinaturas.</summary>
public static class DatabaseProviders
{
    public const string SectionKey = "Database:Provider";
    public const string MongoDb = "MongoDB";
    public const string Oracle = "Oracle";
}

using MongoDB.Driver;
using WhatsAppProject.Entities;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    // Construtor que aceita IMongoClient e o nome do banco de dados
    public MongoDbContext(IMongoClient mongoClient, string databaseName)
    {
        _database = mongoClient.GetDatabase(databaseName);
    }

    public IMongoCollection<FlowDTO> Flows => _database.GetCollection<FlowDTO>("flowsWhatsapp");

}

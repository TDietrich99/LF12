using LF12.Classes.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MongoQuestionService
{
    private readonly IMongoCollection<CrosswordQuestions> _questions;

    public MongoQuestionService(IConfiguration config)
    {
        var connectionString = config["MongoConnection:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("MongoDB connection string is missing in configuration.");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("riddles");
        _questions = database.GetCollection<CrosswordQuestions>("questions");
    }

    /// <summary>
    /// Holt die passende Antwort zur Frage mit gegebener Länge.
    /// </summary>
public async Task<string?> GetSolution(string question, int length)
{
    // 1. String vorbereiten
    question = question?.Trim();
    if (string.IsNullOrWhiteSpace(question))
        return null;

    // 2. Case‑insensitive Filter (Regex)
    var filterQuestion = Builders<CrosswordQuestions>.Filter.Regex(
        q => q.Question, new BsonRegularExpression($"^{Regex.Escape(question)}$", "i"));

    // 3. Länge erst **nach** Treffer prüfen
    var doc = await _questions.Find(filterQuestion).FirstOrDefaultAsync();
    if (doc == null)
        return null;

    return  doc.Answer;
}


    public async Task<List<CrosswordQuestions>> GetAllAsync() => 
        await _questions.Find(_ => true).ToListAsync();

    public async Task<CrosswordQuestions?> GetByIdAsync(int id) =>
        await _questions.Find(q => q.Id == id).FirstOrDefaultAsync();
}

using System;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq; // Für LINQ-Methoden
using System.Xml;
using LF12.Classes.Classes;
using System.Text.Json;

namespace LF12.Classes.Classes
{

    public class Datenbank
    {
        // dung MongoDB
        static MongoClient client = new MongoClient("mongodb://localhost:27017");
        static IMongoDatabase database = client.GetDatabase("MongoDBAnwsers");
        static IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("Answers");
        public static string[] GetPossibleAnswers(string question, int length)
        {
            var ret = new List<string>();
            var filter = Builders<BsonDocument>.Filter.Eq("Length", length);
            var length_long_questions = Datenbank.collection.Find(filter).ToList();
            List<(BsonDocument Dokument, int Distanz)> passendeÜbereinstimmungen = new List<(BsonDocument, int)>();

            int maxDistanz = 2 + question.Length / 5;
            foreach (var dokument in length_long_questions)
            {
                string dbFrage = dokument["Question"].AsString;

                // Prüfen, ob die Länge des Dokuments mit der Eingabe übereinstimmt
                if (dokument["Length"].AsInt32 != length)
                    continue;

                // Berechne die Levenshtein-Distanz
                int distanz = BerechneLevenshteinDistanz(question, dbFrage, maxDistanz);

                // Prüfe, ob die Distanz innerhalb der erlaubten Distanz liegt
                if (distanz <= maxDistanz)
                {
                    passendeÜbereinstimmungen.Add((dokument, distanz));
                }
            }
            if (passendeÜbereinstimmungen.Count > 0)
            {
                int kleinsteDistanz = passendeÜbereinstimmungen.Min(x => x.Distanz);
                var besteÜbereinstimmungen = passendeÜbereinstimmungen
                    .Where(x => x.Distanz == kleinsteDistanz)
                    .Select(x => x.Dokument);

                if (kleinsteDistanz == 0)
                {
                    // Perfekte Übereinstimmungen
                    foreach (var übereinstimmung in besteÜbereinstimmungen)
                    {
                        string answer = übereinstimmung["Answer"].AsString;
                        ret.Add(answer);
                    }
                }
                else
                {
                    // Übereinstimmungen mit Abweichungen
                    foreach (var übereinstimmung in besteÜbereinstimmungen)
                    {
                        string answer = übereinstimmung["Answer"].AsString;
                        ret.Add(answer);
                    }
                }
            }
            return ret.ToArray();
        }

        //Distanz = Abweichung an Zeichen der eingegebenen Frage zur Frage in der Datenbank, Zeichen ändern, hinzufügen oder entfernen//
        static int BerechneLevenshteinDistanz(string s1, string s2, int maxDistanz)
        {
            s1 = s1.ToLower(); // Konvertiere in Kleinbuchstaben
            s2 = s2.ToLower();

            int n = s1.Length;
            int m = s2.Length;

            if (Math.Abs(n - m) > maxDistanz)
            {
                return maxDistanz + 1;
            }

            var d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; i++)
            {
                d[i, 0] = i;
            }

            for (int j = 0; j <= m; j++)
            {
                d[0, j] = j;
            }

            for (int i = 1; i <= n; i++)
            {
                int minDistInRow = int.MaxValue;
                for (int j = 1; j <= m; j++)
                {
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                    minDistInRow = Math.Min(minDistInRow, d[i, j]);
                }
                if (minDistInRow > maxDistanz)
                {
                    return maxDistanz + 1;
                }
            }

            return d[n, m];
        }
    }
}
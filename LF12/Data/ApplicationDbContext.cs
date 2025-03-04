using LF12.Classes.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml;

namespace LF12.Data;
public class ApplicationDbContext : DbContext
{

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=app.db");
    }
    public DbSet<LF12.Classes.Models.CrosswordQuestions> CrosswordQuestions { get; set; }

}


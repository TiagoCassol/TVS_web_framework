using BeachAula4.Entidades;
using Microsoft.EntityFrameworkCore;

namespace BeachAula4.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Quadra> Quadras => Set<Quadra>();

        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=beachtennis.db");
        }
        
    }
}

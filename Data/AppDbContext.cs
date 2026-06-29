using Microsoft.EntityFrameworkCore;
using Back_Calendary.Models;

namespace Back_Calendary.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        //Calendary
        
    }
}
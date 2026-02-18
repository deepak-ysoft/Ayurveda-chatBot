using Ayurveda_chatBot.Models;
using Microsoft.EntityFrameworkCore;

namespace Ayurveda_chatBot.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Dosha> Doshas { get; set; }
        public DbSet<Herb> Herbs { get; set; }
        public DbSet<ChatHistory> ChatHistories { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<UserSavedHerb> UserSavedHerbs { get; set; }
    }
}

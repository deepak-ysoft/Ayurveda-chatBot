using System.ComponentModel.DataAnnotations;

namespace Ayurveda_chatBot.Models
{
    public abstract class BaseEntity
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        

        public DateTime? UpdatedAt { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Back_Calendary.Models;

namespace Back_Calendary.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("email")]
        public string Email { get; set; }

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; }
        [Required]
        [Column("full_name")]
        public string FullName { get; set; }

        [Column("image_url")]
        public string? ImageUrl { get; set; }
    }
}
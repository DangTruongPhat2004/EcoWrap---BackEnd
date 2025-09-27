using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoWrap.DAL.Entities
{
    [Table("EmailVerifications")]
    public partial class EmailVerification
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("User_Id")]
        public int UserId { get; set; }

        [Column("VerificationCode")]
        [StringLength(10)]
        public string VerificationCode { get; set; } = null!;

        [Column("Expiration", TypeName = "datetime")]
        public DateTime Expiration { get; set; }

        [Column("IsUsed")]
        public bool IsUsed { get; set; }

        [Column("CreatedAt", TypeName = "datetime")]
        public DateTime CreatedAt { get; set; }

        [ForeignKey("UserId")]
        [InverseProperty("EmailVerifications")]
        public virtual User User { get; set; } = null!;
    }
}
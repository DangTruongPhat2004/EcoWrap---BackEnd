using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Index("Username", Name = "UQ__Users__536C85E4CD0BC7F5", IsUnique = true)]
[Index("Email", Name = "UQ__Users__A9D10534A5DEE639", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("User_id")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("Password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [StringLength(50)]
    public string Email { get; set; } = null!;

    [Column("Full_name")]
    [StringLength(50)]
    public string? FullName { get; set; }

    [Column("Phone_number")]
    [StringLength(20)]
    [Unicode(false)]
    public string? PhoneNumber { get; set; }

    [Column("Profile_picture")]
    public string? ProfilePicture { get; set; }

    [Column("Registration_date", TypeName = "datetime")]
    public DateTime? RegistrationDate { get; set; }

    [Column("IsVerified")]
    public bool IsVerified { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();

    [InverseProperty("User")]
    public virtual ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

    [InverseProperty("User")]
    public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

    [InverseProperty("User")]
    public virtual ICollection<Seller> Sellers { get; set; } = new List<Seller>();

    [InverseProperty("User")]
    public virtual ICollection<EmailVerification> EmailVerifications { get; set; } = new List<EmailVerification>();

}

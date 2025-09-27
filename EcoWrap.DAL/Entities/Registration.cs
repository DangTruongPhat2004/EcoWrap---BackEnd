using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Registration")]
public partial class Registration
{
    [Key]
    [Column("Registration_id")]
    [StringLength(10)]
    [Unicode(false)]
    public string RegistrationId { get; set; } = null!;

    [Column("User_id")]
    public int UserId { get; set; }

    [Column("Company_name")]
    [StringLength(100)]
    public string? CompanyName { get; set; }

    [Column("Company_address")]
    [StringLength(255)]
    public string? CompanyAddress { get; set; }

    [Column("Company_phone")]
    [StringLength(50)]
    [Unicode(false)]
    public string? CompanyPhone { get; set; }

    [Column("Personal_id_image")]
    [Unicode(false)]
    public string? PersonalIdImage { get; set; }

    [Column("License_id")]
    [StringLength(50)]
    [Unicode(false)]
    public string? LicenseId { get; set; }

    [Column("Document_path")]
    [Unicode(false)]
    public string? DocumentPath { get; set; }

    [Column("Requested_date", TypeName = "datetime")]
    public DateTime? RequestedDate { get; set; }

    [Column("Approval_status")]
    [StringLength(20)]
    public string ApprovalStatus { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Registrations")]
    public virtual User User { get; set; } = null!;
}

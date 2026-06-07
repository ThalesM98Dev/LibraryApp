using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Generated;

[Index("Email", Name = "UQ__Members__A9D105348890381D", IsUnique = true)]
public partial class Member
{
    [Key]
    public int MemberId { get; set; }

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    [StringLength(200)]
    public string Email { get; set; } = null!;

    public DateTime MemberSince { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Member")]
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}

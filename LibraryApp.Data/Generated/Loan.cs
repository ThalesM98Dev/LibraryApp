using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Generated;

public partial class Loan
{
    [Key]
    public int LoanId { get; set; }

    public int MemberId { get; set; }

    public int CopyId { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Fine { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("CopyId")]
    [InverseProperty("Loans")]
    public virtual BookCopy Copy { get; set; } = null!;

    [ForeignKey("MemberId")]
    [InverseProperty("Loans")]
    public virtual Member Member { get; set; } = null!;
}

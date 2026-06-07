using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Generated;

public partial class BookCopy
{
    [Key]
    public int CopyId { get; set; }

    public int BookId { get; set; }

    [StringLength(20)]
    public string Condition { get; set; } = null!;

    public bool IsAvailable { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("BookId")]
    [InverseProperty("BookCopies")]
    public virtual Book Book { get; set; } = null!;

    [InverseProperty("Copy")]
    public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Generated;

[Index("Title", Name = "IX_Books_Title")]
[Index("Isbn", Name = "UQ__Books__447D36EA3DCF67F3", IsUnique = true)]
public partial class Book
{
    [Key]
    public int BookId { get; set; }

    [Column("ISBN")]
    [StringLength(20)]
    public string Isbn { get; set; } = null!;

    [StringLength(300)]
    public string Title { get; set; } = null!;

    public short PublicationYear { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Book")]
    public virtual ICollection<BookCopy> BookCopies { get; set; } = new List<BookCopy>();

    [ForeignKey("BookId")]
    [InverseProperty("Books")]
    public virtual ICollection<Author> Authors { get; set; } = new List<Author>();

    [ForeignKey("BookId")]
    [InverseProperty("Books")]
    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();
}

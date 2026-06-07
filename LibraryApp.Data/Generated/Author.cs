using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Data.Generated;

public partial class Author
{
    [Key]
    public int AuthorId { get; set; }

    [StringLength(150)]
    public string FullName { get; set; } = null!;

    public string? Biography { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("Authors")]
    public virtual ICollection<Book> Books { get; set; } = new List<Book>();
}

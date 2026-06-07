using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryApp.Data
{
    public class LibraryContextFactory : IDesignTimeDbContextFactory<LibraryContext>
    {
        public LibraryContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../LibraryApp.Api"))
                .AddJsonFile("appsettings.json")
                .Build();

            var opts = new DbContextOptionsBuilder<LibraryContext>();
            opts.UseSqlServer(config.GetConnectionString("LibraryDb"));
            return new LibraryContext(opts.Options);
        }
    }
}

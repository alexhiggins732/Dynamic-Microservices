using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace EFGetStarted.AspNetCore.NewDb.Models
{
    public class ServiceDbContext<IDbModel>: DbContext
        where IDbModel: class
    {
        public ServiceDbContext(DbContextOptions<ServiceDbContext<IDbModel>> options)
         : base(options)
        {
            this.Database.EnsureCreated();

        }

        public DbSet<IDbModel> DbEntries { get; set; }
    }


    public class BloggingContext : DbContext
    {
        public BloggingContext(DbContextOptions<BloggingContext> options)
            : base(options)
        { }

        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Post> Posts { get; set; }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; }

        public ICollection<Post> Posts { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        public int BlogId { get; set; }
        public Blog Blog { get; set; }
    }
}


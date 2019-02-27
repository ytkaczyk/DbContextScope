using System;

namespace DbContextScope.Tests.DatabaseContext
{
  public class Post
  {
    public Guid Id { get; set; }
    public string Title { get; set; }
    public User Author { get; set; }

    public override string ToString()
    {
      return $"Id: {Id} | Author: {Author.Name} ({Author.Id}) | Title: {Title}";
    }
  }
}
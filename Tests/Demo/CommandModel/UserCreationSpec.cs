using System;

namespace DbContextScopeTests.Demo.CommandModel
{
  /// <summary>
  /// Specifications of the CreateUser command. Defines the properties of a new user.
  /// </summary>
  public class UserCreationSpec
  {
    public UserCreationSpec(string name, string email)
    {
      Id = Guid.NewGuid();
      Name = name;
      Email = email;
    }

    /// <summary>
    /// The Id automatically generated for this user.
    /// </summary>
    public Guid Id { get; protected set; }

    public string Name { get; protected set; }
    public string Email { get; protected set; }

    public void Validate()
    {
      // [...]
    }
  }
}

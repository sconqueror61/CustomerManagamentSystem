using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class User
{
    public int Id { get; set; }

    public int UserTypeId { get; set; }

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string SurName { get; set; } = null!;

    public string Adress { get; set; } = null!;

    public string? Country { get; set; }
}

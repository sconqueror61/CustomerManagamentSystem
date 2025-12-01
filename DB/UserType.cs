using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class UserType
{
    public int Id { get; set; }

    public string UserType1 { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}

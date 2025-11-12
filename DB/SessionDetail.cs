using System;
using System.Collections.Generic;

namespace CustomerManagementSystem.DB;

public partial class SessionDetail
{
    public int Id { get; set; }

    public string Action { get; set; } = null!;

    public string Path { get; set; } = null!;

    public int? SessionId { get; set; }
}

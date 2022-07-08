using System;
using System.Collections.Generic;

namespace СourseworkBackend.Models
{
    public partial class UserFileSystemStructure
    {
        public int Id { get; set; }
        public int Userid { get; set; }
        public string SerializedStructure { get; set; } = null!;
        public DateTime Timestamp { get; set; }

        public virtual User User { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;

namespace СourseworkBackend.Models
{
    public partial class Session
    {
        public int Id { get; set; }
        public int Userid { get; set; }
        public string Ip { get; set; } = null!;
        public byte[] Token { get; set; } = null!;
        public DateTime LastTokenUseTime { get; set; }
        public virtual User User { get; set; } = null!;
    }
}

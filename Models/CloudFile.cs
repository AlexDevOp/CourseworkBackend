using System;
using System.Collections.Generic;

namespace СourseworkBackend.Models
{
    public partial class CloudFile
    {
        public int Fileid { get; set; }
        public int Userid { get; set; }
        public long Lenght { get; set; }
        public string ServersideName { get; set; } = null!;
        public string ServersideToken { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;

namespace СourseworkBackend.Models
{
    public partial class User
    {
        public User()
        {
            CloudFiles = new HashSet<CloudFile>();
            Sessions = new HashSet<Session>();
            UsersFileSystemStructures = new HashSet<UserFileSystemStructure>();
            UsersTrustedDevices = new HashSet<UserTrustedDevice>();
        }

        public int Id { get; set; }
        public string Login { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public byte[] PassFingerprint { get; set; } = null!;

        public virtual ICollection<CloudFile> CloudFiles { get; set; }
        public virtual ICollection<Session> Sessions { get; set; }
        public virtual ICollection<UserFileSystemStructure> UsersFileSystemStructures { get; set; }
        public virtual ICollection<UserTrustedDevice> UsersTrustedDevices { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace СourseworkBackend.Models
{
    public partial class UserTrustedDevice
    {
        public int Deviceid { get; set; }
        public int Userid { get; set; }
        public byte[] DeviceFingerprint { get; set; } = null!;
        public byte[] DeviceToken { get; set; } = null!;

        public virtual User User { get; set; } = null!;
    }
}

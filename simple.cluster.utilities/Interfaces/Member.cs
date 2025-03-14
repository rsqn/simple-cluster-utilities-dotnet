namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model
{
    using System;

    public class Member : ICloneable
    {
        public string Id { get; set; }
        public long StartTime { get; set; }
        public long Expires { get; set; }
        public long Ts { get; set; }
        public long Ttl { get; set; }
        public string Tag { get; set; }

        public bool IsExpired()
        {
            return Expires < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public void UpdateTimestamps()
        {
            Ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Expires = Ts + Ttl;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Member other = (Member)obj;
            return string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"Member{{id='{Id}', tag='{Tag}'}}";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
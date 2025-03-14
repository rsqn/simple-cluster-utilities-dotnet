namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Drivers
{
    using System.Collections.Generic;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;

    public interface IClusterViewDriver
    {
        List<Member> FetchMembersWithTag(string tag);
        List<Member> FetchMembersWithAnyTag();
        void Remove(Member member);
        void SendHeartBeat(Member self);
    }
}
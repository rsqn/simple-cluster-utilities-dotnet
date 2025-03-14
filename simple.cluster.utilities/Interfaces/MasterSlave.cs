namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave
{
    using System.Collections.Generic;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;
    using Tech.Rsqn.Useful.Things.Concurrency; // Assuming you have a .NET equivalent

    public interface IClusterView
    {
        bool IsMaster { get; }
        bool IsReady { get; }
        Member GetSelf();
        bool ClusterContainsMemberId(string memberId);
        void OnReady(ICallback cb); // Adjust ICallback based on your .NET implementation
        List<Member> GetMembers();
        List<Member> GetNonMembersWithTag(string tag);
    }

    // Assuming you have a corresponding Member model:
    namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model {
        public class Member {
            // Define your Member properties here, for example:
            public string MemberId {get; set;}
            public string Tag {get; set;}
            // ... other properties
        }
    }

    // Assuming you have a corresponding Callback interface:
    namespace Tech.Rsqn.Useful.Things.Concurrency {
        public interface ICallback
        {
            void Invoke(); // Or whatever method your callback needs.
        }
    }
}
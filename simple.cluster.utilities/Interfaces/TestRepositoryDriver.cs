namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Drivers;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;

    public class TestRepositoryDriver : IClusterViewDriver
    {
        private ConcurrentBag<Member> _collection = new ConcurrentBag<Member>();

        public List<Member> FetchMembersWithTag(string tag)
        {
            return _collection.Where(m => string.Equals(tag, m.Tag, StringComparison.Ordinal)).ToList();
        }

        public List<Member> FetchMembersWithAnyTag()
        {
            return _collection.ToList();
        }

        public void Remove(Member member)
        {
            Console.WriteLine($"Removing a member collection is {_collection.Count}");

            // Find and remove the matching member.
            var toRemove = _collection.FirstOrDefault(m => string.Equals(m.Id, member.Id, StringComparison.Ordinal));
            if (toRemove != null)
            {
                _collection = new ConcurrentBag<Member>(_collection.Where(m => !string.Equals(m.Id, member.Id, StringComparison.Ordinal)));
            }
            Console.WriteLine($"Removing a member collection now {_collection.Count}");
        }

        public void SendHeartBeat(Member self)
        {
            // Create a new Member instance to avoid modifying the original.
            var copy = (Member)self.Clone();
            _collection.Add(copy);
        }
    }
}
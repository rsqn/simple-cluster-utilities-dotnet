namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Drivers
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;
    using Tech.Rsqn.SimpleClusterUtilities.Topics; // Make sure you have the .NET equivalent

    public class TopicClusterViewDriver : IClusterViewDriver
    {
        private readonly ILogger<TopicClusterViewDriver> _logger;
        private ITopic _topic;
        private List<Member> _allKnownMembers;

        public TopicClusterViewDriver(ILogger<TopicClusterViewDriver> logger)
        {
            _logger = logger;
            _allKnownMembers = new List<Member>();
        }

        public void SetTopic(ITopic topic)
        {
            _topic = topic;
        }

        public void Init()
        {
            _topic.Subscribe(m => OnEvent((IMessage<Member>)m));
        }

        private void OnEvent(IMessage<Member> msg)
        {
            Member member = msg.Payload;

            _logger.LogTrace("received member {Member} on topic {Topic}", member, _topic);

            lock (_allKnownMembers)
            {
                _allKnownMembers.Remove(member);
                _allKnownMembers.Add(member);
            }
        }

        public void Remove(Member member)
        {
            lock (_allKnownMembers)
            {
                _allKnownMembers.Remove(member);
            }
        }

        public List<Member> FetchMembersWithTag(string tag)
        {
            return _allKnownMembers.Where(m => string.Equals(tag, m.Tag, StringComparison.Ordinal)).ToList();
        }

        public List<Member> FetchMembersWithAnyTag()
        {
            return new List<Member>(_allKnownMembers);
        }

        public void SendHeartBeat(Member member)
        {
            _topic.Publish(new Message<Member>().With(member));

            lock (_allKnownMembers)
            {
                _allKnownMembers.Remove(member);
                _allKnownMembers.Add(member);
            }
        }
    }
}
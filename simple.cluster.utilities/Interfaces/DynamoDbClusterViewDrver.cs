namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Drivers
{
    using Amazon.DynamoDBv2;
    using Amazon.DynamoDBv2.DocumentModel;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;

    public class DynamoDbClusterViewDriver : IClusterViewDriver
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly string _tableName;
        private readonly ILogger<DynamoDbClusterViewDriver> _logger;

        public DynamoDbClusterViewDriver(IAmazonDynamoDB dynamoDbClient, string tableName, ILogger<DynamoDbClusterViewDriver> logger)
        {
            _dynamoDbClient = dynamoDbClient;
            _tableName = tableName;
            _logger = logger;
        }

        public List<Member> FetchMembersWithTag(string tag)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, _tableName);
                var filter = new ScanFilter();
                filter.AddCondition("Tag", ScanOperator.Equal, tag);
                var scanOps = new ScanOperationConfig() { Filter = filter };
                var search = table.Scan(scanOps);
                var results = search.GetNextSet();

                return results.Select(DocumentToMember).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching members with tag: {Tag}", tag);
                return new List<Member>();
            }
        }

        public List<Member> FetchMembersWithAnyTag()
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, _tableName);
                var search = table.Scan(new ScanOperationConfig());
                var results = search.GetNextSet();

                return results.Select(DocumentToMember).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all members");
                return new List<Member>();
            }
        }

        public void Remove(Member member)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, _tableName);
                table.DeleteItem(member.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing member: {MemberId}", member.Id);
            }
        }

        public void SendHeartBeat(Member member)
        {
            try
            {
                var table = Table.LoadTable(_dynamoDbClient, _tableName);
                var document = MemberToDocument(member);
                table.PutItem(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending heartbeat for member: {MemberId}", member.Id);
            }
        }

        private Member DocumentToMember(Document document)
        {
            return new Member
            {
                Id = document["Id"].AsString(),
                StartTime = document["StartTime"].AsLong(),
                Ttl = document["Ttl"].AsLong(),
                Tag = document["Tag"].AsString()
                //Add other properties if they exist.
            };
        }

        private Document MemberToDocument(Member member)
        {
            return new Document
            {
                ["Id"] = member.Id,
                ["StartTime"] = member.StartTime,
                ["Ttl"] = member.Ttl,
                ["Tag"] = member.Tag
                //Add other properties if they exist.
            };
        }
    }
}
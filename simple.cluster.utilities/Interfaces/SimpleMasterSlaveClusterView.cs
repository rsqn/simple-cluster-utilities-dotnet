namespace Tech.Rsqn.SimpleClusterUtilities.MasterSlave
{
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Drivers;
    using Tech.Rsqn.SimpleClusterUtilities.MasterSlave.Model;
    using Tech.Rsqn.Useful.Things.Concurrency; // Ensure this is adapted for .NET
    using Tech.Rsqn.Useful.Things.Identifiers; // Ensure this is adapted for .NET

    public class SimpleMasterSlaveClusterView : IClusterView
    {
        private static readonly string OnReady = "onReady";
        private readonly ILogger<SimpleMasterSlaveClusterView> _logger;
        private readonly IConfiguration _configuration;
        private readonly IClusterViewDriver _driver;
        private readonly INotifier _notifier; // Ensure this is adapted for .NET
        private Member _mySelf;
        private bool _iAmMaster = false;
        private Member _master;
        private long _ttlMs = 30L * 1000L;
        private long _heartbeatMs = 15L * 1000L;
        private long _stabilisationPeriodMs;
        private List<Member> _reportedMembers;
        private bool _keepRunning = false;
        private Thread _thread;
        private string _tag;

        public SimpleMasterSlaveClusterView(ILogger<SimpleMasterSlaveClusterView> logger, IConfiguration configuration, IClusterViewDriver driver, INotifier notifier)
        {
            _logger = logger;
            _configuration = configuration;
            _driver = driver;
            _notifier = notifier;
            _reportedMembers = new List<Member>();
            _tag = _configuration.GetValue("SimpleMasterSlaveClusterView:tag", "default");
            _stabilisationPeriodMs = _heartbeatMs * 2;
            Initialize();
        }

        public void SetTtlMs(long ttlMs)
        {
            _ttlMs = ttlMs;
        }

        public void SetHeartbeatMs(long heartbeatMs)
        {
            _heartbeatMs = heartbeatMs;
        }

        public void SetDriver(IClusterViewDriver driver)
        {
            _driver = driver;
        }

        public void SetTag(string tag)
        {
            _tag = tag;
        }

        private void Initialize()
        {
            _mySelf = new Member
            {
                StartTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Id = "member-" + UIDHelper.Generate(), // Ensure UIDHelper is adapted for .NET
                Ttl = _ttlMs,
                Tag = _tag
            };

            _keepRunning = true;
            _thread = new Thread(RunMainLoop) { IsBackground = true };
            _thread.Start();
        }

        private void RunMainLoop()
        {
            while (_keepRunning)
            {
                try
                {
                    MainLoop();
                    Thread.Sleep(TimeSpan.FromMilliseconds(_heartbeatMs));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main loop");
                }
            }
        }

        public void Stop()
        {
            if (_keepRunning)
            {
                _keepRunning = false;
                try
                {
                    _thread?.Interrupt();
                    _thread?.Join();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error stopping thread");
                }

            }
        }

        private void MainLoop()
        {
            SendHeartbeat();
            DetermineMaster();
        }

        private void DetermineMaster()
        {
            lock (this)
            {
                List<Member> members = _driver.FetchMembersWithTag(_tag);
                members.Where(m => m.IsExpired()).ToList().ForEach(expired => _driver.Remove(expired));
                members = _driver.FetchMembersWithTag(_tag);

                members.Sort((o1, o2) =>
                {
                    int ret = o2.StartTime.CompareTo(o1.StartTime) * -1;
                    if (ret == 0)
                    {
                        ret = o2.Id.CompareTo(o1.Id);
                    }
                    return ret;
                });

                members = members.Distinct().ToList();

                lock (_reportedMembers)
                {
                    _reportedMembers.Clear();
                    _reportedMembers.AddRange(members);
                }

                if (_mySelf.StartTime + _stabilisationPeriodMs > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
                {
                    _logger.LogInformation("In stabilisation period {View}", this);
                    return;
                }

                if (members.Count == 0)
                {
                    _logger.LogWarning("No members? should not get here but this will resolve {Members}", members);
                    return;
                }

                Member detectedMaster = members[0];
                bool logView = false;
                bool firstMasterSelected = false;

                if (_master == null)
                {
                    _logger.LogInformation("First master selection {Master}", detectedMaster);
                    logView = true;
                    firstMasterSelected = true;
                }
                else if (!detectedMaster.Equals(_master))
                {
                    _logger.LogInformation("New master selected {Master}", detectedMaster);
                    logView = true;
                }

                _master = detectedMaster;
                _iAmMaster = _master.Equals(_mySelf);

                _logger.LogTrace("{View}", this);

                if (logView)
                {
                    _logger.LogInformation("{View}", this);
                }

                if (firstMasterSelected)
                {
                    _notifier.Send(OnReady, null);
                    _notifier.RemoveAllListeners();
                }
            }
        }

        private void SendHeartbeat()
        {
            _mySelf.UpdateTimestamps();
            _driver.SendHeartBeat(_mySelf);
        }

        public bool IsMaster => _iAmMaster;

        public bool IsReady => _master != null;

        public Member GetSelf()
        {
            return (Member)_mySelf.Clone();
        }

        public List<Member> GetMembers()
        {
            lock (_reportedMembers)
            {
                return new List<Member>(_reportedMembers);
            }
        }

        public List<Member> GetNonMembersWithTag(string tag)
        {
            return _driver.FetchMembersWithTag(tag);
        }

        public override string ToString()
        {
            return $"ClusterView[iAmMaster({_iAmMaster}) Master({_master}) Self({_mySelf}) Members({_reportedMembers.Count}:{string.Join(",", _reportedMembers)})]";
        }

        public void OnReady(ICallback cb)
        {
            if (IsReady)
            {
                cb.Invoke();
            }
            else
            {
                _notifier.Listen(OnReady, _ => cb.Invoke());
            }
        }
    }
}
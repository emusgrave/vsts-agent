using Microsoft.TeamFoundation.DistributedTask.WebApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;

namespace Microsoft.VisualStudio.Services.Agent
{
    [ServiceLocator(Default = typeof(JobServer))]
    public interface IJobServer : IAgentService
    {
        event EventHandler<TarpitEventArgs> JobServerThrottling;

        Task ConnectAsync(VssConnection jobConnection);

        // logging and console
        Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken);
        Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, IEnumerable<string> lines, CancellationToken cancellationToken);
        Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, String type, String name, Stream uploadStream, CancellationToken cancellationToken);
        Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken);
        Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken);
        Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken);
    }

    public sealed class JobServer : AgentService, IJobServer
    {
        private bool _hasConnection;
        private VssConnection _connection;
        private TaskHttpClient _taskClient;

        public event EventHandler<TarpitEventArgs> JobServerThrottling;

        public async Task ConnectAsync(VssConnection jobConnection)
        {
            _connection = jobConnection;

            TarpitReportHandler.ServerThrottling += ServerTarpit_EventReceived;
            if (!_connection.HasAuthenticated)
            {
                await _connection.ConnectAsync();
            }

            _taskClient = _connection.GetClient<TaskHttpClient>();
            _hasConnection = true;
        }

        private void ServerTarpit_EventReceived(object sender, TarpitEventArgs data)
        {
            if (JobServerThrottling != null)
            {
                JobServerThrottling(this, data);
            }
        }

        private void CheckConnection()
        {
            if (!_hasConnection)
            {
                throw new InvalidOperationException("SetConnection");
            }
        }

        //-----------------------------------------------------------------
        // Feedback: WebConsole, TimelineRecords and Logs
        //-----------------------------------------------------------------

        public Task<TaskLog> AppendLogContentAsync(Guid scopeIdentifier, string hubName, Guid planId, int logId, Stream uploadStream, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.AppendLogContentAsync(scopeIdentifier, hubName, planId, logId, uploadStream, cancellationToken);
        }

        public Task AppendTimelineRecordFeedAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, IEnumerable<string> lines, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.AppendTimelineRecordFeedAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, lines, cancellationToken);
        }

        public Task<TaskAttachment> CreateAttachmentAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, Guid timelineRecordId, string type, string name, Stream uploadStream, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateAttachmentAsync(scopeIdentifier, hubName, planId, timelineId, timelineRecordId, type, name, uploadStream, cancellationToken);
        }

        public Task<TaskLog> CreateLogAsync(Guid scopeIdentifier, string hubName, Guid planId, TaskLog log, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateLogAsync(scopeIdentifier, hubName, planId, log, cancellationToken);
        }

        public Task<Timeline> CreateTimelineAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.CreateTimelineAsync(scopeIdentifier, hubName, planId, new Timeline(timelineId), cancellationToken);
        }

        public Task<List<TimelineRecord>> UpdateTimelineRecordsAsync(Guid scopeIdentifier, string hubName, Guid planId, Guid timelineId, IEnumerable<TimelineRecord> records, CancellationToken cancellationToken)
        {
            CheckConnection();
            return _taskClient.UpdateTimelineRecordsAsync(scopeIdentifier, hubName, planId, timelineId, records, cancellationToken);
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI.Messages;
using EventStore.ClientAPI.SystemData;
using EventStore.ClientAPI.Transport.Tcp;

namespace EventStore.ClientAPI.ClientOperations
{
    internal class ConnectToPersistentSubscriptionOperation : SubscriptionOperation<PersistentEventStoreSubscription>
    {
        private readonly string _groupName;
        private readonly int _bufferSize;
        private readonly string _subscriptionId;

        public ConnectToPersistentSubscriptionOperation(ILogger log, TaskCompletionSource<PersistentEventStoreSubscription> source, string groupName, int bufferSize, string streamId, UserCredentials userCredentials, Action<PersistentEventStoreSubscription, ResolvedEvent> eventAppeared, Action<PersistentEventStoreSubscription, SubscriptionDropReason, Exception> subscriptionDropped, bool verboseLogging, Func<TcpPackageConnection> getConnection)
            : base(log, source, streamId, false, userCredentials, eventAppeared, subscriptionDropped, verboseLogging, getConnection)
        {
            _groupName = groupName;
            _bufferSize = bufferSize;
            _subscriptionId = _streamId + "::" + _groupName;
        }

        protected override TcpPackage CreateSubscriptionPackage()
        {
            var dto = new ClientMessage.ConnectToPersistentSubscription(_groupName, _streamId, _bufferSize);
            return new TcpPackage(TcpCommand.ConnectToPersistentSubscription,
                                  _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                                  _correlationId,
                                  _userCredentials != null ? _userCredentials.Username : null,
                                  _userCredentials != null ? _userCredentials.Password : null,
                                  dto.Serialize());
        }

        protected override bool InspectPackage(TcpPackage package, out InspectionResult result)
        {
            if (package.Command == TcpCommand.PersistentSubscriptionConfirmation)
            {
                var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionConfirmation>();
                        ConfirmSubscription(dto.LastCommitPosition, dto.LastEventNumber);
                        result = new InspectionResult(InspectionDecision.Subscribed, "SubscriptionConfirmation");
                return true;
            }
            if (package.Command == TcpCommand.PersistentSubscriptionStreamEventAppeared)
            {
                var dto = package.Data.Deserialize<ClientMessage.PersistentSubscriptionStreamEventAppeared>();
                EventAppeared(new ResolvedEvent(dto.Event));
                result = new InspectionResult(InspectionDecision.DoNothing, "StreamEventAppeared");
                return true;
            }
            result = null;
            return false;
        }

        protected override PersistentEventStoreSubscription CreateSubscriptionObject(long lastCommitPosition, int? lastEventNumber)
        {
            return new PersistentEventStoreSubscription(this, _streamId, lastCommitPosition, lastEventNumber);
        }

        public void NotifyEventsProcessed(Guid[] processedEvents)
        {
            var dto = new ClientMessage.PersistentSubscriptionAckEvents(
                _subscriptionId,
                processedEvents.Select(x => x.ToByteArray()).ToArray());

            var package = new TcpPackage(TcpCommand.PersistentSubscriptionAckEvents,
                                  _userCredentials != null ? TcpFlags.Authenticated : TcpFlags.None,
                                  _correlationId,
                                  _userCredentials != null ? _userCredentials.Username : null,
                                  _userCredentials != null ? _userCredentials.Password : null,
                                  dto.Serialize());
            EnqueueSend(package);
        }
    }
}
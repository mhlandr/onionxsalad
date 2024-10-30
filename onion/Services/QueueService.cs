using onion.Models;
using System.Collections.Concurrent;

namespace onion.Services
{
    public interface IQueueService
    {
        Guid EnqueueScreenshotRequest(string url);
        ScreenshotRequest GetRequestStatus(Guid id);
        bool TryDequeue(out ScreenshotRequest request);
    }

    public class QueueService : IQueueService
    {
        private readonly ConcurrentQueue<ScreenshotRequest> _queue = new ConcurrentQueue<ScreenshotRequest>();
        private readonly ConcurrentDictionary<Guid, ScreenshotRequest> _requests = new ConcurrentDictionary<Guid, ScreenshotRequest>();

        public Guid EnqueueScreenshotRequest(string url)
        {
            var request = new ScreenshotRequest
            {
                Id = Guid.NewGuid(),
                Url = url,
                RequestedAt = DateTime.UtcNow,
                Status = "Pending"
            };

            _requests[request.Id] = request;
            _queue.Enqueue(request);
            return request.Id;
        }

        public ScreenshotRequest GetRequestStatus(Guid id)
        {
            _requests.TryGetValue(id, out var request);
            return request;
        }

        public bool TryDequeue(out ScreenshotRequest request)
        {
            return _queue.TryDequeue(out request);
        }
    }
}

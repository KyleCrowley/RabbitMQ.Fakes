using RabbitMQ.Client;
using System.Collections.Generic;
using System.Linq;

namespace RabbitMQ.Fakes.DotNetStandard.Models
{
    public class FanoutExchange : Exchange
    {
        private readonly IList<Queue> _queues;

        public FanoutExchange(string name) : base(name, ExchangeType.Fanout)
        {
            _queues = new List<Queue>();
        }

        public override void BindQueue(string bindingKey, Queue queue)
        {
            if (_queues.Any(q => q.Name == queue.Name))
            {
                // TODO: Throw Exception?
                return;
            }

            _queues.Add(queue);
        }

        public override void UnbindQueue(string bindingKey, Queue queue)
        {
            _queues.Remove(queue);
        }

        protected override IEnumerable<Queue> GetQueues(RabbitMessage message)
        {
            // Messages can always be routed, unless there are no bound Queues.
            if (_queues.Count == 0)
            {
                return Enumerable.Empty<Queue>();
            }

            return _queues;
        }
    }
}
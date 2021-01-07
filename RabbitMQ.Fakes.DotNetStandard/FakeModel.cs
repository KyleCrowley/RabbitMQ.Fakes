using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Fakes.DotNetStandard.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Queue = RabbitMQ.Fakes.DotNetStandard.Models.Queue;

namespace RabbitMQ.Fakes.DotNetStandard
{
    public class FakeModel : IModel
    {
        private readonly RabbitServer _server;
        private readonly ConcurrentDictionary<string, IBasicConsumer> _consumers = new ConcurrentDictionary<string, IBasicConsumer>();
        public readonly ConcurrentDictionary<ulong, RabbitMessage> WorkingMessages = new ConcurrentDictionary<ulong, RabbitMessage>();

        private long _lastDeliveryTag;
        private bool _publisherConfirmsEnabled;

        #region Properties

        #region IModel Implementation

        public TimeSpan ContinuationTimeout { get; set; }

        public ulong NextPublishSeqNo { get; set; }

        public bool IsOpen { get; set; }

        public bool IsClosed { get; set; }

        public ShutdownEventArgs CloseReason { get; set; }

        public IBasicConsumer DefaultConsumer { get; set; }

        public int ChannelNumber { get; }

        #endregion IModel Implementation

        #endregion Properties

        #region Event Handlers

        event EventHandler<ShutdownEventArgs> IModel.ModelShutdown
        {
            add { AddedModelShutDownEvent += value; }
            remove { AddedModelShutDownEvent -= value; }
        }

        event EventHandler<CallbackExceptionEventArgs> IModel.CallbackException
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public event EventHandler<BasicReturnEventArgs> BasicReturn;

        event EventHandler<EventArgs> IModel.BasicRecoverOk
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public event EventHandler<BasicNackEventArgs> BasicNacks;

        public event EventHandler<BasicAckEventArgs> BasicAcks;

        event EventHandler<FlowControlEventArgs> IModel.FlowControl
        {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        public EventHandler<ShutdownEventArgs> AddedModelShutDownEvent { get; set; }

        #endregion Event Handlers

        #region Constructors

        public FakeModel(RabbitServer server)
        {
            _server = server;
        }

        #endregion Constructors

        #region Public Methods

        #region IModel Implementation

        public void Abort()
        {
            Abort(ushort.MaxValue, string.Empty);
        }

        public void Abort(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, replyCode, replyText);
        }

        public void BasicAck(ulong deliveryTag, bool multiple)
        {
            if (multiple)
            {
                while (BasicAckSingle(deliveryTag))
                    --deliveryTag;
            }
            else
            {
                BasicAckSingle(deliveryTag);
            }
        }

        public void BasicCancel(string consumerTag)
        {
            _consumers.TryRemove(consumerTag, out var consumer);

            // TODO: Associate consumers with queues so we don't need to loop through *all* queues.
            foreach (var queue in _server.Queues.Values)
            {
                queue.RemoveConsumer(consumerTag);
            }

            if (consumer != null)
            {
                if (consumer is IAsyncBasicConsumer asyncConsumer)
                {
                    asyncConsumer.HandleBasicCancelOk(consumerTag).Wait();
                }
                else
                {
                    consumer.HandleBasicCancelOk(consumerTag);
                }
            }
        }

        public void BasicCancelNoWait(string consumerTag)
        {
            throw new NotImplementedException();
        }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, bool noLocal, bool exclusive, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            _server.Queues.TryGetValue(queue, out var queueInstance);

            if (queueInstance != null)
            {
                _consumers.AddOrUpdate(consumerTag, consumer, (s, basicConsumer) => basicConsumer);
                queueInstance.AddConsumer(consumerTag, consumer);
            }

            return consumerTag;
        }

        public BasicGetResult BasicGet(string queue, bool autoAck)
        {
            _server.Queues.TryGetValue(queue, out var queueInstance);
            if (queueInstance == null) return null;

            RabbitMessage message;
            if (autoAck)
            {
                queueInstance.Messages.TryDequeue(out message);
            }
            else
            {
                queueInstance.Messages.TryPeek(out message);
            }

            if (message == null)
                return null;

            if (autoAck)
            {
                WorkingMessages.TryRemove(message.DeliveryTag, out _);
            }
            else
            {
                RabbitMessage UpdateFunction(ulong key, RabbitMessage existingMessage) => existingMessage;
                WorkingMessages.AddOrUpdate(message.DeliveryTag, message, UpdateFunction);
            }

            return new BasicGetResult(message.DeliveryTag, false, message.Exchange, message.RoutingKey, Convert.ToUInt32(queueInstance.Messages.Count), message.BasicProperties, message.Body);
        }

        public void BasicNack(ulong deliveryTag, bool multiple, bool requeue)
        {
            if (requeue) return;

            foreach (var queue in WorkingMessages.Select(m => m.Value.Queue))
            {
                _server.Queues.TryGetValue(queue, out var queueInstance);

                if (queueInstance != null)
                {
                    queueInstance.Messages = new ConcurrentQueue<RabbitMessage>();
                }
            }

            WorkingMessages.TryRemove(deliveryTag, out var message);
            if (message == null) return;

            // As per the RabbitMQ spec, we need to check if this message should be delivered to a Dead Letter Exchange (DLX) if:
            // 1) The message was NAcked or Rejected
            // 2) Requeue = false
            // See: https://www.rabbitmq.com/dlx.html
            _server.Queues.TryGetValue(message.Queue, out var processingQueue);
            if
            (
                processingQueue.Arguments != null
                    && processingQueue.Arguments.TryGetValue("x-dead-letter-exchange", out var dlx)
                    && _server.Exchanges.TryGetValue((string)dlx, out var exchange)
            )
            {
                // If the "x-dead-letter-routing-key" argument was specified, that key is used instead of the "original" routing key.
                // https://www.rabbitmq.com/dlx.html#routing
                message.RoutingKey = processingQueue.Arguments.TryGetValue("x-dead-letter-routing-key", out var key)
                    ? (string)key
                    : message.RoutingKey;

                exchange.PublishMessage(message);
                return;
            }

            foreach (var workingMessage in WorkingMessages)
            {
                _server.Queues.TryGetValue(workingMessage.Value.Queue, out var queueInstance);

                queueInstance?.PublishMessage(workingMessage.Value);
            }
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, IBasicProperties basicProperties, ReadOnlyMemory<byte> body)
        {
            BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: mandatory, immediate: true, basicProperties: basicProperties, body: body.Span.ToArray());
        }

        public void BasicQos(uint prefetchSize, ushort prefetchCount, bool global)
        {
            throw new NotImplementedException();
        }

        public void BasicRecover(bool requeue)
        {
            if (requeue)
            {
                foreach (var message in WorkingMessages)
                {
                    Queue queueInstance;
                    _server.Queues.TryGetValue(message.Value.Queue, out queueInstance);

                    if (queueInstance != null)
                    {
                        queueInstance.PublishMessage(message.Value);
                    }
                }
            }

            WorkingMessages.Clear();
        }

        public void BasicRecoverAsync(bool requeue)
        {
            BasicRecover(requeue);
        }

        public void BasicReject(ulong deliveryTag, bool requeue)
        {
            BasicNack(deliveryTag: deliveryTag, multiple: false, requeue: requeue);
        }

        public void Close(ushort replyCode, string replyText)
        {
            IsClosed = true;
            IsOpen = false;
            CloseReason = new ShutdownEventArgs(ShutdownInitiator.Library, replyCode, replyText);
        }

        public void Close()
        {
            Close(ushort.MaxValue, string.Empty);
        }

        public void ConfirmSelect()
        {
            _publisherConfirmsEnabled = true;
        }

        public uint ConsumerCount(string queue)
        {
            throw new NotImplementedException();
        }

        public IBasicProperties CreateBasicProperties()
        {
            return new FakeBasicProperties();
        }

        public IBasicPublishBatch CreateBasicPublishBatch()
        {
            throw new NotImplementedException();
        }

        public void ExchangeBind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            // TODO: This is an exchange-to-exchange binding *NOT* a queue-to-exchange binding.
            /*
            Exchange exchange;
            _server.Exchanges.TryGetValue(source, out exchange);

            Queue queue;
            _server.Queues.TryGetValue(destination, out queue);

            var binding = new ExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            if (exchange != null)
                exchange.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
            if (queue != null)
                queue.Bindings.AddOrUpdate(binding.Key, binding, (k, v) => binding);
            */

            throw new NotImplementedException();
        }

        public void ExchangeBindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public void ExchangeDeclare(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            if (_server.Exchanges.ContainsKey(exchange))
            {
                return;
            }

            var exchangeInstance = ExchangeFactory.Instance.GetExchange(exchange, type);
            exchangeInstance.IsDurable = durable;
            exchangeInstance.AutoDelete = autoDelete;
            exchangeInstance.Arguments = arguments as IDictionary;

            _server.Exchanges.TryAdd(exchange, exchangeInstance);
        }

        public void ExchangeDeclareNoWait(string exchange, string type, bool durable, bool autoDelete, IDictionary<string, object> arguments)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: arguments);
        }

        public void ExchangeDeclarePassive(string exchange)
        {
            // In "real" RabbitMQ, this will produce a channel-level exception if the exchange does not exist.
            if (!_server.Exchanges.ContainsKey(exchange))
            {
                // TODO: More specific Exception?
                throw new Exception($"Exchange '{exchange}' does not exist.");
            }
        }

        public void ExchangeDelete(string exchange)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeDeleteNoWait(string exchange, bool ifUnused)
        {
            ExchangeDelete(exchange, ifUnused: false);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            // TODO: This is an exchange-to-exchange binding *NOT* a queue-to-exchange binding.
            /*
            Exchange exchange;
            _server.Exchanges.TryGetValue(source, out exchange);

            Queue queue;
            _server.Queues.TryGetValue(destination, out queue);

            var binding = new ExchangeQueueBinding { Exchange = exchange, Queue = queue, RoutingKey = routingKey };
            ExchangeQueueBinding removedBinding;
            if (exchange != null)
                exchange.Bindings.TryRemove(binding.Key, out removedBinding);
            if (queue != null)
                queue.Bindings.TryRemove(binding.Key, out removedBinding);
            */

            throw new NotImplementedException();
        }

        public void ExchangeUnbindNoWait(string destination, string source, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public uint MessageCount(string queue)
        {
            throw new NotImplementedException();
        }

        public void QueueBind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            if (!_server.Queues.TryGetValue(queue, out var queueInstance))
            {
                throw new InvalidOperationException($"Cannot bind queue '{queue}' to exchange '{exchange}' because the specified queue does not exist.");
            }
            if (!_server.Exchanges.TryGetValue(exchange, out var exchangeInstance))
            {
                throw new InvalidOperationException($"Cannot bind queue '{queue}' to exchange '{exchange}' because the specified exchange does not exist.");
            }

            exchangeInstance.BindQueue(routingKey, queueInstance);
        }

        public void QueueBindNoWait(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            throw new NotImplementedException();
        }

        public QueueDeclareOk QueueDeclare(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            var queueInstance = new Queue
            {
                Name = queue,
                IsDurable = durable,
                IsExclusive = exclusive,
                IsAutoDelete = autoDelete,
                Arguments = arguments
            };

            Func<string, Queue, Queue> updateFunction = (name, existing) => existing;
            _server.Queues.AddOrUpdate(queue, queueInstance, updateFunction);

            var exchangeBinding = new ExchangeQueueBinding
            {
                RoutingKey = queueInstance.Name,
                Exchange = _server.DefaultExchange,
                Queue = queueInstance
            };
            _server.DefaultExchange.BindQueue(queueInstance.Name, queueInstance);

            return new QueueDeclareOk(queue, 0, 0);
        }

        public void QueueDeclareNoWait(string queue, bool durable, bool exclusive, bool autoDelete, IDictionary<string, object> arguments)
        {
            QueueDeclare(queue, durable, exclusive, autoDelete, arguments);
        }

        public QueueDeclareOk QueueDeclarePassive(string queue)
        {
            return QueueDeclare(queue, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public uint QueueDelete(string queue, bool ifUnused, bool ifEmpty)
        {
            Queue instance;
            _server.Queues.TryRemove(queue, out instance);

            return instance != null ? 1u : 0u;
        }

        public void QueueDeleteNoWait(string queue, bool ifUnused, bool ifEmpty)
        {
            QueueDelete(queue, ifUnused: false, ifEmpty: false);
        }

        public uint QueuePurge(string queue)
        {
            Queue instance;
            _server.Queues.TryGetValue(queue, out instance);

            if (instance == null)
                return 0u;

            while (!instance.Messages.IsEmpty)
            {
                RabbitMessage itemToRemove;
                instance.Messages.TryDequeue(out itemToRemove);
            }

            return 1u;
        }

        public void QueueUnbind(string queue, string exchange, string routingKey, IDictionary<string, object> arguments)
        {
            if (!_server.Queues.TryGetValue(queue, out var queueInstance))
            {
                throw new InvalidOperationException($"Cannot unbind queue '{queue}' from exchange '{exchange}' because the specified queue does not exist.");
            }
            if (!_server.Exchanges.TryGetValue(exchange, out var exchangeInstance))
            {
                throw new InvalidOperationException($"Cannot unbind queue '{queue}' from exchange '{exchange}' because the specified exchange does not exist.");
            }

            exchangeInstance.UnbindQueue(routingKey, queueInstance);
        }

        public void TxCommit()
        {
            throw new NotImplementedException();
        }

        public void TxRollback()
        {
            throw new NotImplementedException();
        }

        public void TxSelect()
        {
            throw new NotImplementedException();
        }

        // TODO: Need to determine an actual implementation for the WaitForConfirms* methods.
        // In "real" RabbitMQ, publisher confirms are sent back to the client asynchronously.
        // One use case for these methods is publishing a batch of messages and then waiting for all "pending" responses.
        public bool WaitForConfirms(TimeSpan timeout, out bool timedOut)
        {
            if (!_publisherConfirmsEnabled)
            {
                throw new InvalidOperationException("This channel is not in confirm mode (publisher confirms were not enabled via ConfirmSelect().");
            }

            timedOut = false;
            return true;
        }

        // TODO: Return true when all messages have been acked by the broker. Otherwise, return false.
        public bool WaitForConfirms(TimeSpan timeout) => WaitForConfirms(timeout, out _);

        // TODO: Return true when all messages have been acked by the broker. Otherwise, return false.
        public bool WaitForConfirms() => WaitForConfirms(TimeSpan.Zero);

        // TODO: Should throw an Exception when a nack is received.
        public void WaitForConfirmsOrDie() => WaitForConfirmsOrDie(TimeSpan.Zero);

        // TODO: Should throw an Exception when a nack is received or the timeout has elapsed.
        public void WaitForConfirmsOrDie(TimeSpan timeout) => WaitForConfirms(timeout);

        #endregion IModel Implementation

        #region IDisposable Implementation

        public void Dispose()
        {
        }

        #endregion IDisposable Implementation

        public string BasicConsume(string queue, bool autoAck, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, autoAck: autoAck, consumerTag: Guid.NewGuid().ToString(), noLocal: true, exclusive: false, arguments: null, consumer: consumer);
        }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, autoAck: autoAck, consumerTag: consumerTag, noLocal: true, exclusive: false, arguments: null, consumer: consumer);
        }

        public string BasicConsume(string queue, bool autoAck, string consumerTag, IDictionary<string, object> arguments, IBasicConsumer consumer)
        {
            return BasicConsume(queue: queue, autoAck: autoAck, consumerTag: consumerTag, noLocal: true, exclusive: false, arguments: arguments, consumer: consumer);
        }

        public void BasicPublish(PublicationAddress addr, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: addr.ExchangeName, routingKey: addr.RoutingKey, mandatory: true, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, IBasicProperties basicProperties, byte[] body)
        {
            BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: true, immediate: true, basicProperties: basicProperties, body: body);
        }

        public void BasicPublish(string exchange, string routingKey, bool mandatory, bool immediate, IBasicProperties basicProperties, byte[] body)
        {
            Interlocked.Increment(ref _lastDeliveryTag);
            var deliveryTag = Convert.ToUInt64(_lastDeliveryTag);
            var message = new RabbitMessage
            {
                Exchange = exchange,
                RoutingKey = routingKey,
                Mandatory = mandatory,
                Immediate = immediate,
                BasicProperties = basicProperties,
                Body = body,
                DeliveryTag = deliveryTag
            };
            Func<ulong, RabbitMessage, RabbitMessage> updateFunction = (key, existingMessage) => existingMessage;
            WorkingMessages.AddOrUpdate(deliveryTag, message, updateFunction);

            NextPublishSeqNo++;

            if (!_server.Exchanges.TryGetValue(exchange, out var exchangeInstance))
            {
                throw new InvalidOperationException($"Cannot publish to exchange '{exchange}' as it does not exist.");
            }

            var canRoute = exchangeInstance.PublishMessage(message);

            // We only raise events if publisher confirms are enabled.
            if (_publisherConfirmsEnabled)
            {
                // https://www.rabbitmq.com/confirms.html#when-publishes-are-confirmed
                // Mandatory messages are special case that receive both an basic.ack (or basic.nack) and basic.return.
                // The basic.return is sent *before* the ack/nack.
                if (message.Mandatory && !canRoute)
                {
                    OnMessageReturned(message, exchange, routingKey);
                }

                // An ack (basic.ack) just confirms that the broker received the message.
                // This doesn't mean that the broker was able to *route* the message; that is handled by basic.return (see above).
                OnMessageAcknowledged(message);
            }
        }

        public void ExchangeDeclare(string exchange, string type, bool durable)
        {
            ExchangeDeclare(exchange, type, durable, autoDelete: false, arguments: null);
        }

        public void ExchangeDeclare(string exchange, string type)
        {
            ExchangeDeclare(exchange, type, durable: false, autoDelete: false, arguments: null);
        }

        public void ExchangeDelete(string exchange, bool ifUnused)
        {
            Exchange removedExchange;
            _server.Exchanges.TryRemove(exchange, out removedExchange);
        }

        public void ExchangeBind(string destination, string source, string routingKey)
        {
            ExchangeBind(destination: destination, source: source, routingKey: routingKey, arguments: null);
        }

        public void ExchangeUnbind(string destination, string source, string routingKey)
        {
            ExchangeUnbind(destination: destination, source: source, routingKey: routingKey, arguments: null);
        }

        public QueueDeclareOk QueueDeclare()
        {
            var name = Guid.NewGuid().ToString();
            return QueueDeclare(name, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }

        public uint QueueDelete(string queue)
        {
            return QueueDelete(queue, ifUnused: false, ifEmpty: false);
        }

        public IEnumerable<RabbitMessage> GetMessagesOnQueue(string queueName)
        {
            Queue queueInstance;
            _server.Queues.TryGetValue(queueName, out queueInstance);

            if (queueInstance == null)
                return new List<RabbitMessage>();

            return queueInstance.Messages;
        }

        #endregion Public Methods

        #region Private Methods

        private bool BasicAckSingle(ulong deliveryTag)
        {
            WorkingMessages.TryRemove(deliveryTag, out var message);

            if (message != null)
            {
                _server.Queues.TryGetValue(message.Queue, out var queue);

                if (queue != null)
                {
                    queue.Messages.TryDequeue(out message);
                }
            }

            return message != null;
        }

        private void OnMessageAcknowledged(RabbitMessage message)
        {
            // TODO: Handle multiple publisher acknowledgment.
            BasicAcks?.Invoke(this, new BasicAckEventArgs { DeliveryTag = message.DeliveryTag, Multiple = false });
        }

        private void OnMessageReturned(RabbitMessage message, string exchange, string routingKey)
        {
            BasicReturn?.Invoke(this, new BasicReturnEventArgs
            {
                BasicProperties = message.BasicProperties,
                Body = message.Body,
                Exchange = exchange,
                ReplyCode = 0,
                ReplyText = "Message could not be delivered to any queues bound to the exchange.",
                RoutingKey = routingKey
            });
        }

        #endregion Private Methods
    }
}
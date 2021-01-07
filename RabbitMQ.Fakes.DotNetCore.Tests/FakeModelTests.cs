using FluentAssertions;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Fakes.DotNetStandard;
using RabbitMQ.Fakes.DotNetStandard.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Queue = RabbitMQ.Fakes.DotNetStandard.Models.Queue;

namespace RabbitMQ.Fakes.DotNetCore.Tests
{
    [TestFixture]
    public class FakeModelTests
    {
        private bool _wasCalled;

        [Test]
        public void AddModelShutDownEvent_EventIsTracked()
        {
            //arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            //act
            Assert.That(model.AddedModelShutDownEvent, Is.Null);
            ((IModel)model).ModelShutdown += (args, e) => { _wasCalled = true; };

            //Assert
            Assert.That(model.AddedModelShutDownEvent, Is.Not.Null);
        }

        [Test]
        public void AddModelShutDownEvent_EventIsRemoved()
        {
            //arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);
            EventHandler<ShutdownEventArgs> onModelShutdown = (args, e) => { _wasCalled = true; };
            ((IModel)model).ModelShutdown += onModelShutdown;

            //act
            Assert.That(model.AddedModelShutDownEvent, Is.Not.Null);
            ((IModel)model).ModelShutdown -= onModelShutdown;

            //Assert
            Assert.That(model.AddedModelShutDownEvent, Is.Null);
        }

        [Test]
        public void CreateBasicProperties_ReturnsBasicProperties()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            var result = model.CreateBasicProperties();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ExchangeDeclare_AllArguments_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = ExchangeType.Direct;
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            node.Exchanges.Should().ContainKey(exchangeName, "an exchange was declared with name {0}", exchangeName);

            var exchange = node.Exchanges[exchangeName];
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameTypeAndDurable_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = ExchangeType.Direct;
            const bool isDurable = true;

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType, durable: isDurable);

            // Assert
            node.Exchanges.Should().ContainKey(exchangeName, "an exchange was declared with name {0}", exchangeName);

            var exchange = node.Exchanges[exchangeName];
            AssertExchangeDetails(exchange, exchangeName, false, null, isDurable, exchangeType);
        }

        [Test]
        public void ExchangeDeclare_WithNameType_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = ExchangeType.Direct;

            // Act
            model.ExchangeDeclare(exchange: exchangeName, type: exchangeType);

            // Assert
            node.Exchanges.Should().ContainKey(exchangeName, "an exchange was declared with name {0}", exchangeName);

            var exchange = node.Exchanges[exchangeName];
            AssertExchangeDetails(exchange, exchangeName, false, null, false, exchangeType);
        }

        [Test]
        public void ExchangeDeclarePassive_NoExchangeWithName_ThrowsException()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";

            // Act
            Action action = () => model.ExchangeDeclarePassive(exchange: exchangeName);

            // Assert
            action.Should().Throw<Exception>("the specified exchange was not previously declared non-passively");
        }

        [Test]
        public void ExchangeDeclareNoWait_CreatesExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            const string exchangeType = ExchangeType.Direct;
            const bool isDurable = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.ExchangeDeclareNoWait(exchange: exchangeName, type: exchangeType, durable: isDurable, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            node.Exchanges.Should().ContainKey(exchangeName, "an exchange was declared with name {0}", exchangeName);

            var exchange = node.Exchanges[exchangeName];
            AssertExchangeDetails(exchange, exchangeName, isAutoDelete, arguments, isDurable, exchangeType);
        }

        private void AssertExchangeDetails(Exchange exchange, string exchangeName, bool isAutoDelete, IDictionary<string, object> arguments, bool isDurable, string exchangeType)
        {
            exchange.AutoDelete.Should().Be(isAutoDelete);
            exchange.Arguments.Should().BeEquivalentTo(arguments);
            exchange.IsDurable.Should().Be(isDurable);
            exchange.Name.Should().Be(exchangeName);
            exchange.Type.Should().Be(exchangeType);
        }

        [Test]
        public void ExchangeDelete_NameOnlyExchangeExists_RemovesTheExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);

            // Act
            model.ExchangeDelete(exchange: exchangeName);

            // Assert
            node.Exchanges.Should().NotContainKey(exchangeName, "the exchange with name {0} was deleted", exchangeName);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDelete_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);

            // Act
            model.ExchangeDelete(exchange: exchangeName, ifUnused: ifUnused);

            // Assert
            node.Exchanges.Should().NotContainKey(exchangeName, "the exchange with name {0} was deleted", exchangeName);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExchangeDeleteNoWait_ExchangeExists_RemovesTheExchange(bool ifUnused)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);

            // Act
            model.ExchangeDeleteNoWait(exchange: exchangeName, ifUnused: ifUnused);

            // Assert
            node.Exchanges.Should().NotContainKey(exchangeName, "the exchange with name {0} was deleted", exchangeName);
        }

        [Test]
        public void ExchangeDelete_ExchangeDoesNotExists_DoesNothing()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string exchangeName = "someExchange";
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);

            // Act
            model.ExchangeDelete(exchange: "someOtherExchange");

            // Assert
            node.Exchanges.Should().NotContainKey("someOtherExchange", "the exchange with name {0} never existed", "someOtherExchange");
        }

        // TODO: Fix once ExchangeBind is implemented properly.
        /*
        [Test]
        public void ExchangeBind_BindsAnExchangeToAnotherExchange()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            model.QueueDeclarePassive(queueName);

            // Act
            model.ExchangeBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            var exchange = node.Exchanges[exchangeName].As<DirectExchange>();
            exchange.Bindings.Should().ContainKey(routingKey);

            var binding = exchange.Bindings[routingKey];
            binding.Should().HaveCount(1);
            binding[0].Name.Should().Be(queueName);
        }
        */

        [Test]
        public void QueueBind_BindsAnExchangeToAQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);

            // Act
            model.QueueBind(queueName, exchangeName, routingKey, arguments);

            // Assert
            var exchange = node.Exchanges[exchangeName].As<DirectExchange>();
            exchange.Bindings.Should().ContainKey(routingKey);

            var binding = exchange.Bindings[routingKey];
            binding.Should().HaveCount(1);
            binding[0].Name.Should().Be(queueName);
        }

        // TODO: Fix once ExchangeBind/ExchangeUnbind are implemented properly.
        /*
        [Test]
        public void ExchangeUnbind_RemovesBinding()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.ExchangeBind(exchangeName, queueName, routingKey, arguments);

            // Act
            model.ExchangeUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            node.Exchanges[exchangeName].As<DirectExchange>().Bindings.Should().BeEmpty();
            node.Queues[queueName].Bindings.Should().BeEmpty();
        }
        */

        [Test]
        public void QueueUnbind_RemovesBinding()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const string exchangeName = "someExchange";
            const string routingKey = "someRoutingKey";
            var arguments = new Dictionary<string, object>();

            model.ExchangeDeclare(exchangeName, "direct");
            model.QueueDeclarePassive(queueName);
            model.QueueBind(queueName, exchangeName, routingKey, arguments);

            // Act
            model.QueueUnbind(queueName, exchangeName, routingKey, arguments);

            // Assert
            node.Exchanges[exchangeName].As<DirectExchange>().Bindings.Should().BeEmpty();
        }

        [Test]
        public void QueueDeclare_NoArguments_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.QueueDeclare();

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));
        }

        [Test]
        public void QueueDeclarePassive_WithName_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "myQueue";

            // Act
            model.QueueDeclarePassive(queueName);

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));
            Assert.That(node.Queues.First().Key, Is.EqualTo(queueName));
            Assert.That(node.Queues.First().Value.Name, Is.EqualTo(queueName));
        }

        [Test]
        public void QueueDeclare_CreatesQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someQueue";
            const bool isDurable = true;
            const bool isExclusive = true;
            const bool isAutoDelete = false;
            var arguments = new Dictionary<string, object>();

            // Act
            model.QueueDeclare(queue: queueName, durable: isDurable, exclusive: isExclusive, autoDelete: isAutoDelete, arguments: arguments);

            // Assert
            Assert.That(node.Queues, Has.Count.EqualTo(1));

            node.DefaultExchange.Bindings.Should().ContainKey(queueName, "all newly declared queues are implicitly bound to the default exchange");

            var queue = node.Queues.First();
            AssertQueueDetails(queue, queueName, isAutoDelete, arguments, isDurable, isExclusive);
        }

        private static void AssertQueueDetails(KeyValuePair<string, Queue> queue, string exchangeName, bool isAutoDelete, Dictionary<string, object> arguments, bool isDurable, bool isExclusive)
        {
            Assert.That(queue.Key, Is.EqualTo(exchangeName));
            Assert.That(queue.Value.IsAutoDelete, Is.EqualTo(isAutoDelete));
            Assert.That(queue.Value.Arguments, Is.EqualTo(arguments));
            Assert.That(queue.Value.IsDurable, Is.EqualTo(isDurable));
            Assert.That(queue.Value.Name, Is.EqualTo(exchangeName));
            Assert.That(queue.Value.IsExclusive, Is.EqualTo(isExclusive));
        }

        [Test]
        public void QueueDelete_NameOnly_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDelete(queueName);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDelete_WithArguments_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDelete(queueName, true, true);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDeleteNoWait_WithArguments_DeletesTheQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            const string queueName = "someName";
            model.QueueDeclare(queueName, true, true, true, null);

            // Act
            model.QueueDeleteNoWait(queueName, true, true);

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueueDelete_NonExistentQueue_DoesNothing()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.QueueDelete("someQueue");

            // Assert
            Assert.That(node.Queues, Is.Empty);
        }

        [Test]
        public void QueuePurge_RemovesAllMessagesFromQueue()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_other_queue");
            node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_other_queue"].Messages.Enqueue(new RabbitMessage());

            model.QueueDeclarePassive("my_queue");
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());
            node.Queues["my_queue"].Messages.Enqueue(new RabbitMessage());

            // Act
            model.QueuePurge("my_queue");

            // Assert
            Assert.That(node.Queues["my_queue"].Messages, Is.Empty);
            Assert.That(node.Queues["my_other_queue"].Messages, Is.Not.Empty);
        }

        [Test]
        public void Close_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Close();

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Close_WithArguments_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Close(5, "some message");

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Abort();

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void Abort_WithArguments_ClosesTheChannel()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            model.Abort(5, "some message");

            // Assert
            Assert.That(model.IsClosed, Is.True);
            Assert.That(model.IsOpen, Is.False);
            Assert.That(model.CloseReason, Is.Not.Null);
        }

        [Test]
        public void BasicPublish_PublishesMessage()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            // Act
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(1);
            AssertEqual(node.Queues["my_queue"].Messages.First().Body, encodedMessage);
        }

        [Test]
        public void BasicAck()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            // Act
            var deliveryTag = model.WorkingMessages.First().Key;
            model.BasicAck(deliveryTag, false);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(0);
        }

        [Test]
        public void BasicAckMultiple()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            var message2 = "hello world, too!!";
            var encodedMessage2 = Encoding.ASCII.GetBytes(message2);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage2);
            model.BasicConsume("my_queue", true, new EventingBasicConsumer(model));

            // Act
            var deliveryTag = model.WorkingMessages.Last().Key;
            model.BasicAck(deliveryTag, true);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(0);
        }

        [Test]
        public void BasicGet_MessageOnQueue_GetsMessage()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            AssertEqual(response.Body, encodedMessage);
            response.DeliveryTag.Should().BeGreaterThan(0);
        }

        [Test]
        public void BasicGet_NoMessageOnQueue_ReturnsNull()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclarePassive("my_queue");

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            Assert.That(response, Is.Null);
        }

        [Test]
        public void BasicGet_NoQueue_ReturnsNull()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            // Act
            var response = model.BasicGet("my_queue", false);

            // Assert
            Assert.That(response, Is.Null);
        }

        [TestCase(true, 1, TestName = "If requeue param to BasicNack is true, the message that is nacked should remain in Rabbit")]
        [TestCase(false, 0, TestName = "If requeue param to BasicNack is false, the message that is nacked should be removed from Rabbit")]
        public void Nacking_Message_Should_Not_Reenqueue_Brand_New_Message(bool requeue, int expectedMessageCount)
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicConsume("my_queue", false, new EventingBasicConsumer(model));

            // Act
            var deliveryTag = model.WorkingMessages.First().Key;
            model.BasicNack(deliveryTag, false, requeue);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(expectedMessageCount);
            model.WorkingMessages.Count.Should().Be(expectedMessageCount);
        }

        [TestCase(true, 0, TestName = "BasicGet WITH auto-ack SHOULD remove the message from the queue")]
        [TestCase(false, 1, TestName = "BasicGet with NO auto-ack should NOT remove the message from the queue")]
        public void BasicGet_Should_Not_Remove_The_Message_From_Queue_If_Not_Acked(bool autoAck, int expectedMessageCount)
        {
            // arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var encodedMessage = Encoding.ASCII.GetBytes("hello world!");
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            // act
            var message = model.BasicGet("my_queue", autoAck);

            // assert
            AssertEqual(message.Body, encodedMessage);
            node.Queues["my_queue"].Messages.Count.Should().Be(expectedMessageCount);
            model.WorkingMessages.Count.Should().Be(expectedMessageCount);
        }

        [Test]
        public void BasicRejectDLX()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("dead_letter_exchange", ExchangeType.Direct);
            model.QueueDeclare("error_queue");
            model.QueueBind("error_queue", "dead_letter_exchange", "error_queue");

            model.QueueDeclare("my_queue", arguments: new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", "dead_letter_exchange" },
                { "x-dead-letter-routing-key", "error_queue" }
            });

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            var rabbitMessage = model.BasicGet("my_queue", false);

            // Act
            model.BasicReject(rabbitMessage.DeliveryTag, requeue: false);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(0);
            node.Queues["error_queue"].Messages.Count.Should().Be(1);
        }

        [Test]
        public void BasicRejectNoDLX()
        {
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.ExchangeDeclare("dead_letter_exchange", ExchangeType.Direct);
            model.QueueDeclare("error_queue");
            model.QueueBind("error_queue", "dead_letter_exchange", "error_queue");

            model.QueueDeclare("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            var rabbitMessage = model.BasicGet("my_queue", false);

            // Act
            model.BasicReject(rabbitMessage.DeliveryTag, requeue: false);

            // Assert
            node.Queues["my_queue"].Messages.Count.Should().Be(0);
            node.Queues["error_queue"].Messages.Count.Should().Be(0);
        }

        [Test]
        public void BasicCancel_CancelledBeforeSecondMessageDelivered_DeliversOnlyFirstMessage()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            var count = 0;
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (ch, ea) => { count++; };

            var tag = model.BasicConsume("my_queue", true, consumer);

            // Act
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicCancel(tag);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);

            // Assert
            count.Should().Be(1);
        }

        [Test]
        public void BasicCancel_CancelledAfterSecondMessageDelivered_DeliversBothMessages()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var message = "hello world!";
            var encodedMessage = Encoding.ASCII.GetBytes(message);

            var count = 0;
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (ch, ea) => { count++; };

            var tag = model.BasicConsume("my_queue", true, consumer);

            // Act
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, encodedMessage);
            model.BasicCancel(tag);

            // Assert
            count.Should().Be(2);
        }

        [Test]
        public void BasicPublish_IAsyncBasicConsumer_DeliversMessageToConsumer()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var count = 0;
            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += async (ch, ea) =>
            {
                count++;
                await Task.FromResult(0);
            };

            var tag = model.BasicConsume("my_queue", true, consumer);

            // Act
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            count.Should().Be(1);
        }

        [Test]
        public void BasicPublish_MultipleIAsyncBasicConsumers_DeliversMessageToConsumersRoundRobin()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var count = 0;
            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += async (ch, ea) =>
            {
                count++;
                await Task.FromResult(0);
            };

            var count1 = 0;
            var consumer1 = new AsyncEventingBasicConsumer(model);
            consumer1.Received += async (ch, ea) =>
            {
                count1++;
                await Task.FromResult(0);
            };

            var tag = model.BasicConsume("my_queue", true, consumer);
            var tag1 = model.BasicConsume("my_queue", true, consumer1);

            // Act
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello Other World!"));

            // Assert
            count.Should().Be(1);
            count1.Should().Be(1);
        }

        [Test]
        public void BasicConsume_MessagesInQueueBeforeSubscription_DeliversExistingMessagesToConsumer()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var count = 0;
            var consumer = new EventingBasicConsumer(model);
            consumer.Received += (ch, ea) =>
            {
                count++;
            };

            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Act
            var tag = model.BasicConsume("my_queue", true, consumer);

            // Assert
            count.Should().Be(1);
        }

        [Test]
        public void BasicCancel_AsyncConsumer_DoesntDeliverMessagesAfterCancellation()
        {
            // Arrange
            var node = new RabbitServer();
            var model = new FakeModel(node);

            model.QueueDeclare("my_queue");

            var count = 0;
            var consumer = new AsyncEventingBasicConsumer(model);
            consumer.Received += async (ch, ea) =>
            {
                count++;
                Task.FromResult(0);
            };

            var tag = model.BasicConsume("my_queue", true, consumer);

            // Act
            model.BasicCancel(tag);
            model.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            count.Should().Be(0);
        }

        [Test]
        public void BasicPublish_PublisherConfirmsDisabled_DoesNotRaiseEvents()
        {
            // Arrange
            var node = new RabbitServer();
            var channel = new FakeModel(node);

            channel.QueueDeclare("my_queue");

            using var monitoredChannel = channel.Monitor();

            // Act
            channel.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            monitoredChannel.Should().NotRaise("BasicAcks");
            monitoredChannel.Should().NotRaise("BasicReturn");
        }

        [Test]
        public void BasicPublish_PublisherConfirmsEnabledNotMandatoryNoMatchingQueue_RaisesBasicAcksEventOnly()
        {
            // Arrange
            var node = new RabbitServer();
            var channel = new FakeModel(node);

            channel.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            channel.QueueDeclare("my_queue");
            channel.QueueBind("my_queue", "my_exchange", "foobar");

            channel.ConfirmSelect();

            using var monitoredChannel = channel.Monitor();

            // Act
            channel.BasicPublish("my_exchange", "fizzbuzz", false, true, null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            monitoredChannel.Should().Raise("BasicAcks");
            monitoredChannel.Should().NotRaise("BasicReturn");
        }

        [Test]
        public void BasicPublish_PublisherConfirmsEnabledNoMatchingQueue_RaisesBasicReturnEvent()
        {
            // Arrange
            var node = new RabbitServer();
            var channel = new FakeModel(node);

            channel.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            channel.QueueDeclare("my_queue");
            channel.QueueBind("my_queue", "my_exchange", "foobar");

            channel.ConfirmSelect();

            using var monitoredChannel = channel.Monitor();

            // Act
            channel.BasicPublish("my_exchange", "fizzbuzz", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            monitoredChannel.Should().Raise("BasicReturn").WithArgs<BasicReturnEventArgs>(a => a.Exchange == "my_exchange");
            monitoredChannel.Should().Raise("BasicAcks");
        }

        [Test]
        public void BasicPublish_PublisherConfirmsEnabled_RaisesBasicAcksEvent()
        {
            // Arrange
            var node = new RabbitServer();
            var channel = new FakeModel(node);

            channel.QueueDeclare("my_queue");

            channel.ConfirmSelect();

            using var monitoredChannel = channel.Monitor();

            // Act
            channel.BasicPublish(node.DefaultExchange.Name, "my_queue", null, Encoding.ASCII.GetBytes("Hello World!"));

            // Assert
            monitoredChannel.Should().Raise("BasicAcks");
        }

        [Test]
        public void WaitForConfirms_PublisherConfirmsDisabled_ThrowsInvalidOperationException()
        {
            // Arrange
            var node = new RabbitServer();
            var sut = new FakeModel(node);

            // Act
            Action action = () => sut.WaitForConfirms();

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void WaitForConfirms_PublisherConfirmsEnabled_ReturnsTrue()
        {
            // Arrange
            var node = new RabbitServer();
            var sut = new FakeModel(node);

            sut.ConfirmSelect();

            // Act
            var allAcked = sut.WaitForConfirms();

            // Assert
            allAcked.Should().BeTrue();
        }

        private void AssertEqual(ReadOnlyMemory<byte> actual, ReadOnlySpan<byte> expected)
        {
            actual.Span.SequenceEqual(expected).Should().BeTrue();
        }
    }
}
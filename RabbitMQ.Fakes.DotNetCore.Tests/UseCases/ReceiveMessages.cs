using FluentAssertions;
using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Fakes.DotNetStandard;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQ.Fakes.DotNetCore.Tests.UseCases
{
    [TestFixture]
    public class ReceiveMessages
    {
        [Test]
        public void ReceiveMessagesOnQueue()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            InitializeQueue("my_queue", rabbitServer);
            SendMessage(rabbitServer, rabbitServer.DefaultExchange.Name, "my_queue", "hello_world");

            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            // Act
            var message = channel.BasicGet("my_queue", autoAck: false);

            // Assert
            message.Should().NotBeNull();
            var messageBody = Encoding.ASCII.GetString(message.Body.ToArray());

            messageBody.Should().Be("hello_world");

            rabbitServer.Queues["my_queue"].Messages.Count.Should().Be(1);
            channel.BasicAck(message.DeliveryTag, multiple: false);
            rabbitServer.Queues["my_queue"].Messages.Count.Should().Be(0);
        }

        [Test]
        public void ReceiveMessagesOnQueue_AutoAckEnabled()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            InitializeQueue("my_queue", rabbitServer);
            SendMessage(rabbitServer, rabbitServer.DefaultExchange.Name, "my_queue", "hello_world");

            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            // Act
            var message = channel.BasicGet("my_queue", autoAck: true);

            // Assert
            message.Should().NotBeNull();
            rabbitServer.Queues["my_queue"].Messages.Count.Should().Be(0);
        }

        [Test]
        public void ReceiveMessagesOnQueueWithBasicProperties()
        {
            // Arrange
            var rabbitServer = new RabbitServer();

            InitializeQueue("my_queue", rabbitServer);
            var basicProperties = new FakeBasicProperties
            {
                Headers = new Dictionary<string, object>() { { "TestKey", "TestValue" } },
                CorrelationId = Guid.NewGuid().ToString(),
                ReplyTo = "TestQueue",
                Timestamp = new AmqpTimestamp(123456),
                ReplyToAddress = new PublicationAddress("exchangeType", "excahngeName", "routingKey"),
                ClusterId = "1",
                ContentEncoding = "encoding",
                ContentType = "type",
                DeliveryMode = 1,
                Expiration = "none",
                MessageId = "id",
                Priority = 1,
                Type = "type",
                UserId = "1",
                AppId = "1"
            };

            SendMessage(rabbitServer, rabbitServer.DefaultExchange.Name, "my_queue", "hello_world", basicProperties);
            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            // Act
            var message = channel.BasicGet("my_queue", autoAck: false);

            // Assert
            message.Should().NotBeNull();
            var messageBody = Encoding.ASCII.GetString(message.Body.ToArray());

            messageBody.Should().Be("hello_world");

            var actualBasicProperties = message.BasicProperties;

            actualBasicProperties.Should().BeEquivalentTo(basicProperties);

            //channel.BasicAck(message.DeliveryTag, multiple: false);
        }

        private void SendMessage(RabbitServer rabbitServer, string exchange, string routingKey, string message, IBasicProperties basicProperties = null)
        {
            using var connection = new FakeConnectionFactory(rabbitServer).CreateConnection();
            using var channel = connection.CreateModel();

            var messageBody = Encoding.ASCII.GetBytes(message);

            channel.BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: false, basicProperties: basicProperties, body: messageBody);
        }

        private void InitializeQueue(string queueName, RabbitServer server)
        {
            using var connection = new FakeConnectionFactory(server).CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
        }
    }
}
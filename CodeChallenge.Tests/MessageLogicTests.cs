using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CodeChallenge.Api.Tests
{
    public class MessageLogicTests
    {
        private readonly Mock<IMessageRepository> _mockRepository;
        private readonly MessageLogic _logic;
        private readonly Guid _organizationId;
        private readonly Guid _messageId;

        public MessageLogicTests()
        {
            _mockRepository = new Mock<IMessageRepository>();
            _logic = new MessageLogic(_mockRepository.Object);
            _organizationId = Guid.NewGuid();
            _messageId = Guid.NewGuid();
        }

        [Fact]
        public async Task CreateMessageAsync_Success_ReturnsSuccess()
        {
            // Arrange
            var request = new CreateMessageRequest
            {
                Title = "Test Message",
                Content = "This is a valid content for testing"
            };

            _mockRepository.Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
                .ReturnsAsync((Message?)null);

            _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Message>()))
                .ReturnsAsync((Message m) => m);

            // Act
            var result = await _logic.CreateMessageAsync(_organizationId, request);

            // Assert
            result.Should().BeOfType<Success>();

            _mockRepository.Verify(r => r.GetByTitleAsync(_organizationId, request.Title), Times.Once);
            _mockRepository.Verify(r => r.CreateAsync(It.Is<Message>(m =>
                m.OrganizationId == _organizationId &&
                m.Title == request.Title &&
                m.Content == request.Content)), Times.Once);
        }

        [Fact]
        public async Task CreateMessageAsync_DuplicateTitle_ReturnsConflict()
        {
            // Arrange
            var request = new CreateMessageRequest
            {
                Title = "Duplicate Title",
                Content = "This is a valid content for testing"
            };

            var existingMessage = new Message
            {
                Id = _messageId,
                OrganizationId = _organizationId,
                Title = "Duplicate Title"
            };

            _mockRepository.Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
                .ReturnsAsync(existingMessage);

            // Act
            var result = await _logic.CreateMessageAsync(_organizationId, request);

            // Assert
            result.Should().BeOfType<Conflict>();
            var conflict = result as Conflict;
            conflict.Should().NotBeNull();

            _mockRepository.Verify(r => r.CreateAsync(It.IsAny<Message>()), Times.Never);
        }

        [Fact]
        public async Task CreateMessageAsync_NullTitle_ReturnsValidationError()
        {
            // Arrange
            var request = new CreateMessageRequest
            {
                Title = null!, // Null title
                Content = "Valid content that is more than 10 characters"
            };

            _mockRepository.Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
                .ReturnsAsync((Message?)null);

            // Act
            var result = await _logic.CreateMessageAsync(_organizationId, request);

            // Assert
            result.Should().BeOfType<ValidationError>();
        }

        [Fact]
        public async Task CreateMessageAsync_EmptyTitle_ReturnsValidationError()
        {
            // Arrange
            var request = new CreateMessageRequest
            {
                Title = "", // Empty title
                Content = "Valid content that is more than 10 characters"
            };

            _mockRepository.Setup(r => r.GetByTitleAsync(_organizationId, request.Title))
                .ReturnsAsync((Message?)null);

            // Act
            var result = await _logic.CreateMessageAsync(_organizationId, request);

            // Assert
            result.Should().BeOfType<ValidationError>();
        }

        [Fact]
        public async Task UpdateMessageAsync_NonExistentMessage_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateMessageRequest
            {
                Title = "Updated Title",
                Content = "Updated content"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(_organizationId, _messageId))
                .ReturnsAsync((Message?)null);

            // Act
            var result = await _logic.UpdateMessageAsync(_organizationId, _messageId, request);

            // Assert
            result.Should().BeOfType<NotFound>();
            var notFound = result as NotFound;
            notFound.Should().NotBeNull();

            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Message>()), Times.Never);
        }

        [Fact]
        public async Task DeleteMessageAsync_NonExistentMessage_ReturnsNotFound()
        {
            // Arrange
            _mockRepository.Setup(r => r.DeleteAsync(_organizationId, _messageId))
                .ReturnsAsync(false);

            // Act
            var result = await _logic.DeleteMessageAsync(_organizationId, _messageId);

            // Assert
            result.Should().BeOfType<NotFound>();
            var notFound = result as NotFound;
            notFound.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllMessagesAsync_EmptyOrganizationId_ReturnsEmpty()
        {
            // Arrange
            var emptyOrganizationId = Guid.Empty;

            // Act
            var result = await _logic.GetAllMessagesAsync(emptyOrganizationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();

            _mockRepository.Verify(r => r.GetAllByOrganizationAsync(emptyOrganizationId), Times.Never);
        }

        [Fact]
        public async Task GetAllMessagesAsync_ValidOrganization_ReturnsMessages()
        {
            // Arrange
            var messages = new List<Message>
            {
                new() { Id = Guid.NewGuid(), OrganizationId = _organizationId, Title = "Test 1" },
                new() { Id = Guid.NewGuid(), OrganizationId = _organizationId, Title = "Test 2" }
            };

            _mockRepository.Setup(r => r.GetAllByOrganizationAsync(_organizationId))
                .ReturnsAsync(messages);

            // Act
            var result = await _logic.GetAllMessagesAsync(_organizationId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(messages);

            _mockRepository.Verify(r => r.GetAllByOrganizationAsync(_organizationId), Times.Once);
        }

        [Fact]
        public async Task GetMessageAsync_EmptyId_ReturnsNull()
        {
            // Arrange
            var emptyId = Guid.Empty;

            // Act
            var result = await _logic.GetMessageAsync(_organizationId, emptyId);

            // Assert
            result.Should().BeNull();

            _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, emptyId), Times.Never);
        }

        [Fact]
        public async Task GetMessageAsync_ValidId_ReturnsMessage()
        {
            // Arrange
            var message = new Message
            {
                Id = _messageId,
                OrganizationId = _organizationId,
                Title = "Test Message"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(_organizationId, _messageId))
                .ReturnsAsync(message);

            // Act
            var result = await _logic.GetMessageAsync(_organizationId, _messageId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(_messageId);
            result.Title.Should().Be("Test Message");

            _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, _messageId), Times.Once);
        }

        [Fact]
        public async Task GetMessageAsync_WrongOrganization_ReturnsNull()
        {
            // Arrange
            var differentOrganizationId = Guid.NewGuid();
            var message = new Message
            {
                Id = _messageId,
                OrganizationId = differentOrganizationId, // Different organization
                Title = "Test Message"
            };

            _mockRepository.Setup(r => r.GetByIdAsync(_organizationId, _messageId))
                .ReturnsAsync((Message?)null);

            // Act
            var result = await _logic.GetMessageAsync(_organizationId, _messageId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateMessageAsync_Success_ReturnsUpdated()
        {
            // Arrange
            var request = new UpdateMessageRequest
            {
                Title = "Updated Title",
                Content = "Updated content"
            };

            var existingMessage = new Message
            {
                Id = _messageId,
                OrganizationId = _organizationId,
                Title = "Original Title",
                Content = "Original content"
            };

            var updatedMessage = new Message
            {
                Id = _messageId,
                OrganizationId = _organizationId,
                Title = request.Title,
                Content = request.Content
            };

            _mockRepository.Setup(r => r.GetByIdAsync(_organizationId, _messageId))
                .ReturnsAsync(existingMessage);

            _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Message>()))
                .ReturnsAsync(updatedMessage);

            // Act
            var result = await _logic.UpdateMessageAsync(_organizationId, _messageId, request);

            // Assert
            result.Should().BeOfType<Updated>();

            _mockRepository.Verify(r => r.GetByIdAsync(_organizationId, _messageId), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(It.Is<Message>(m =>
                m.Id == _messageId &&
                m.Title == request.Title &&
                m.Content == request.Content)), Times.Once);
        }

        [Fact]
        public async Task DeleteMessageAsync_Success_ReturnsDeleted()
        {
            // Arrange
            _mockRepository.Setup(r => r.DeleteAsync(_organizationId, _messageId))
                .ReturnsAsync(true);

            // Act
            var result = await _logic.DeleteMessageAsync(_organizationId, _messageId);

            // Assert
            result.Should().BeOfType<Deleted>();

            _mockRepository.Verify(r => r.DeleteAsync(_organizationId, _messageId), Times.Once);
        }

        [Fact]
        public async Task GetMessageAsync_MessageNotFound_ReturnsNull()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetByIdAsync(_organizationId, _messageId))
                .ReturnsAsync((Message?)null);

            // Act
            var result = await _logic.GetMessageAsync(_organizationId, _messageId);

            // Assert
            result.Should().BeNull();
        }
    }
}
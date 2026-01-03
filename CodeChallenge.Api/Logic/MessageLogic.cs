using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;

namespace CodeChallenge.Api.Logic
{
    public class MessageLogic : IMessageLogic
    {
        private readonly IMessageRepository _messageRepository;

        public MessageLogic(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<Result> CreateMessageAsync(Guid organizationId, CreateMessageRequest request)
        {
            // Add validation
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return new ValidationError(new Dictionary<string, string[]>
        {
            { "Title", new[] { "Title is required." } }
        });
            }

            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return new ValidationError(new Dictionary<string, string[]>
        {
            { "Content", new[] { "Content is required." } }
        });
            }

            if (request.Title.Length < 3 || request.Title.Length > 200)
            {
                return new ValidationError(new Dictionary<string, string[]>
        {
            { "Title", new[] { "Title must be between 3 and 200 characters." } }
        });
            }

            if (request.Content.Length < 10 || request.Content.Length > 1000)
            {
                return new ValidationError(new Dictionary<string, string[]>
        {
            { "Content", new[] { "Content must be between 10 and 1000 characters." } }
        });
            }

            if (await _messageRepository.GetByTitleAsync(organizationId, request.Title) != null)
            {
                return new Conflict("A message with the same title already exists.");
            }
            else
            {
                var message = new Message
                {
                    OrganizationId = organizationId,
                    Title = request.Title,
                    Content = request.Content
                };

                await _messageRepository.CreateAsync(message);
                return new Success();
            }
        }

        public async Task<Result> DeleteMessageAsync(Guid organizationId, Guid id)
        {
            bool wasDeleted = await _messageRepository.DeleteAsync(organizationId, id);

            if (wasDeleted)
            {
                return new Deleted();
            }

            return new NotFound("Message not found");
        }

        public Task<IEnumerable<Message>> GetAllMessagesAsync(Guid organizationId)
        {
            if (organizationId == Guid.Empty)
            {
                return Task.FromResult<IEnumerable<Message>>(Array.Empty<Message>());
            }
            else
            {
                return _messageRepository.GetAllByOrganizationAsync(organizationId);
            }
        }

        public Task<Message?> GetMessageAsync(Guid organizationId, Guid id)
        {
            if (id == Guid.Empty)
            {
                return Task.FromResult<Message?>(null);
            }
            else
            {
                return _messageRepository.GetByIdAsync(organizationId, id);
            }
        }

        public async Task<Result> UpdateMessageAsync(Guid organizationId, Guid id, UpdateMessageRequest request)
        {
            var existingMessage = await _messageRepository.GetByIdAsync(organizationId, id);

            if (existingMessage == null)
            {
                return new NotFound("Message not found");
            }

            existingMessage.Title = request.Title;
            existingMessage.Content = request.Content;

            var updatedMessage = await _messageRepository.UpdateAsync(existingMessage);

            return new Updated();
        }
    }
}
using CodeChallenge.Api.Logic;
using CodeChallenge.Api.Models;
using CodeChallenge.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CodeChallenge.Api.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageLogic _messagelogic;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IMessageLogic messageLogic, ILogger<MessagesController> logger)
    {
        _messagelogic = messageLogic;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetAll(Guid organizationId)
    {
        var messages = await _messagelogic.GetAllMessagesAsync(organizationId);
        if (messages == null || !messages.Any())
        {
            _logger.LogInformation("No messages found for organization {OrganizationId}", organizationId);
            return NotFound();
        }
        return Ok(messages);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Message>> GetById(Guid organizationId, Guid id)
    {
        var message = await _messagelogic.GetMessageAsync(organizationId, id);
        if (message == null)
        {
            _logger.LogInformation("Message with ID {MessageId} not found for organization {OrganizationId}", id, organizationId);
            return NotFound();
        }
        return Ok(message);
    }

    [HttpPost]
    public async Task<ActionResult<Message>> Create(Guid organizationId, [FromBody] CreateMessageRequest request)
    {
        var result = await _messagelogic.CreateMessageAsync(organizationId, request);
        if (result is Success)
        {
            return Ok(result);
        }
        else
        {
            _logger.LogWarning("Failed to create message for organization {OrganizationId}", organizationId);
            return BadRequest();
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid organizationId, Guid id, [FromBody] UpdateMessageRequest request)
    {
        var result = await _messagelogic.UpdateMessageAsync(organizationId, id, request);
        if (result is Success)
        {
            return Ok(result);
        }
        else
        {
            _logger.LogWarning("Failed to update message with ID {MessageId} for organization {OrganizationId}", id, organizationId);
            return BadRequest();
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        var result = await _messagelogic.DeleteMessageAsync(organizationId, id);
        if (result is Deleted)
        {
            return Ok(result);
        }
        else
        {
            _logger.LogWarning("Failed to delete message with ID {MessageId} for organization {OrganizationId}", id, organizationId);
            return NotFound();
        }
    }
}
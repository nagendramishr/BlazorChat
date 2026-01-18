using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using src.Models;

namespace src.Authorization;

public class ConversationAuthorizationHandler : AuthorizationHandler<ConversationOwnerRequirement, Conversation>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, 
                                                   ConversationOwnerRequirement requirement, 
                                                   Conversation resource)
    {
        var currentUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // If we have a user ID and it matches the conversation's user ID
        if (currentUserId != null && resource.UserId == currentUserId)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

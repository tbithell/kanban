using System.Security.Claims;
using Kanban.Domain;
using Kanban.Domain.Exceptions;

namespace Kanban.AntiCorruption.Adapters;

public sealed class GoogleIdentityAdapter
{
    public GoogleIdentity Extract(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var sub = principal.FindFirst("sub")?.Value
                  ?? throw new ExternalServiceException(
                      "google.missing_sub",
                      "Google identity is missing the 'sub' claim.");

        var email = principal.FindFirst("email")?.Value
                    ?? throw new ExternalServiceException(
                        "google.missing_email",
                        "Google identity is missing the 'email' claim.");

        var displayName = principal.FindFirst("name")?.Value ?? email;

        return new GoogleIdentity(sub, email, displayName);
    }
}

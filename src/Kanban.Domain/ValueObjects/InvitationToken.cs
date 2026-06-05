using System.Security.Cryptography;
using System.Text;
using FluentValidation;

namespace Kanban.Domain.ValueObjects;

public sealed record InvitationToken
{
    public string Raw { get; }
    public string Hash { get; }

    private InvitationToken(string raw, string hash)
    {
        Raw = raw;
        Hash = hash;
    }

    public static InvitationToken Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = ComputeHash(raw);
        return new InvitationToken(raw, hash);
    }

    public static string HashRaw(string rawToken)
    {
        new InlineValidator<string> { v => v.RuleFor(x => x).NotEmpty().WithName("rawToken") }.ValidateAndThrow(rawToken);
        return ComputeHash(rawToken);
    }

    private static string ComputeHash(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLower();
}

using System.Text.Json.Serialization;

namespace Kanban.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BoardRoleDto { Owner, Member, Viewer }

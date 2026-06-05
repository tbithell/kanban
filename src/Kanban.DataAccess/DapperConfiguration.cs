using System.Data;
using System.Globalization;
using Dapper;
using Kanban.Domain.Enums;

namespace Kanban.DataAccess;

public static class DapperConfiguration
{
    private static bool _registered;
    private static readonly Lock _lock = new();

    public static void RegisterTypeHandlers()
    {
        if (_registered) return;
        lock (_lock)
        {
            if (_registered) return;
            SqlMapper.AddTypeHandler(new GuidHandler());
            SqlMapper.AddTypeHandler(new NullableGuidHandler());
            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new NullableDateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new SystemRoleHandler());
            SqlMapper.AddTypeHandler(new AuthEventTypeHandler());
            SqlMapper.AddTypeHandler(new BoardRoleHandler());
            SqlMapper.AddTypeHandler(new NullableBoardRoleHandler());
            _registered = true;
        }
    }

    private sealed class GuidHandler : SqlMapper.TypeHandler<Guid>
    {
        public override Guid Parse(object value) => Guid.Parse((string)value);

        public override void SetValue(IDbDataParameter parameter, Guid value)
            => parameter.Value = value.ToString("D");
    }

    private sealed class NullableGuidHandler : SqlMapper.TypeHandler<Guid?>
    {
        public override Guid? Parse(object value)
            => value is null or DBNull ? null : Guid.Parse((string)value);

        public override void SetValue(IDbDataParameter parameter, Guid? value)
            => parameter.Value = value.HasValue ? value.Value.ToString("D") : DBNull.Value;
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.Parse((string)value, null, DateTimeStyles.RoundtripKind);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            => parameter.Value = value.ToString("o");
    }

    private sealed class NullableDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
    {
        public override DateTimeOffset? Parse(object value)
            => value is null or DBNull
                ? null
                : DateTimeOffset.Parse((string)value, null, DateTimeStyles.RoundtripKind);

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
            => parameter.Value = value.HasValue ? value.Value.ToString("o") : (object)DBNull.Value;
    }

    private sealed class SystemRoleHandler : SqlMapper.TypeHandler<SystemRole>
    {
        public override SystemRole Parse(object value) => Enum.Parse<SystemRole>((string)value);

        public override void SetValue(IDbDataParameter parameter, SystemRole value)
            => parameter.Value = value.ToString();
    }

    private sealed class AuthEventTypeHandler : SqlMapper.TypeHandler<AuthEventType>
    {
        public override AuthEventType Parse(object value) => Enum.Parse<AuthEventType>((string)value);

        public override void SetValue(IDbDataParameter parameter, AuthEventType value)
            => parameter.Value = value.ToString();
    }

    private sealed class BoardRoleHandler : SqlMapper.TypeHandler<BoardRole>
    {
        public override BoardRole Parse(object value) => Enum.Parse<BoardRole>((string)value);

        public override void SetValue(IDbDataParameter parameter, BoardRole value)
            => parameter.Value = value.ToString();
    }

    private sealed class NullableBoardRoleHandler : SqlMapper.TypeHandler<BoardRole?>
    {
        public override BoardRole? Parse(object value)
            => value is null or DBNull ? null : Enum.Parse<BoardRole>((string)value);

        public override void SetValue(IDbDataParameter parameter, BoardRole? value)
            => parameter.Value = value.HasValue ? value.Value.ToString() : (object)DBNull.Value;
    }
}

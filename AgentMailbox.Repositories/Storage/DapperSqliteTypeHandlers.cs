using System.Data;
using System.Globalization;
using AgentMailbox.Core.Models;
using Dapper;

namespace AgentMailbox.Repositories.Storage;

public static class DapperSqliteTypeHandlers
{
    private static readonly object SyncRoot = new();
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (_registered)
            {
                return;
            }

            SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new NullableDateTimeOffsetHandler());
            SqlMapper.AddTypeHandler(new OutboundMailStatusHandler());
            SqlMapper.AddTypeHandler(new NullableOutboundMailStatusHandler());
            SqlMapper.AddTypeHandler(new InboundMailStatusHandler());
            SqlMapper.AddTypeHandler(new NullableInboundMailStatusHandler());

            _registered = true;
        }
    }

    private static DateTimeOffset ParseDateTimeOffset(object value)
    {
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            DateTime dateTime => new DateTimeOffset(dateTime),
            string text => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => throw new DataException($"Cannot convert '{value.GetType()}' to {nameof(DateTimeOffset)}.")
        };
    }

    private static OutboundMailStatus ParseOutboundMailStatus(object value)
    {
        return value switch
        {
            OutboundMailStatus status => status,
            string text => Enum.Parse<OutboundMailStatus>(text, ignoreCase: true),
            long number => (OutboundMailStatus)number,
            int number => (OutboundMailStatus)number,
            _ => throw new DataException($"Cannot convert '{value.GetType()}' to {nameof(OutboundMailStatus)}.")
        };
    }

    private static InboundMailStatus ParseInboundMailStatus(object value)
    {
        return value switch
        {
            InboundMailStatus status => status,
            string text => Enum.Parse<InboundMailStatus>(text, ignoreCase: true),
            long number => (InboundMailStatus)number,
            int number => (InboundMailStatus)number,
            _ => throw new DataException($"Cannot convert '{value.GetType()}' to {nameof(InboundMailStatus)}.")
        };
    }

    private sealed class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString("O", CultureInfo.InvariantCulture);
        }

        public override DateTimeOffset Parse(object value)
        {
            return ParseDateTimeOffset(value);
        }
    }

    private sealed class NullableDateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset?>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset? value)
        {
            parameter.Value = value is null
                ? DBNull.Value
                : value.Value.ToString("O", CultureInfo.InvariantCulture);
        }

        public override DateTimeOffset? Parse(object value)
        {
            if (value is DBNull)
            {
                return null;
            }

            return ParseDateTimeOffset(value);
        }
    }

    private sealed class OutboundMailStatusHandler : SqlMapper.TypeHandler<OutboundMailStatus>
    {
        public override void SetValue(IDbDataParameter parameter, OutboundMailStatus value)
        {
            parameter.Value = value.ToString();
        }

        public override OutboundMailStatus Parse(object value)
        {
            return ParseOutboundMailStatus(value);
        }
    }

    private sealed class NullableOutboundMailStatusHandler : SqlMapper.TypeHandler<OutboundMailStatus?>
    {
        public override void SetValue(IDbDataParameter parameter, OutboundMailStatus? value)
        {
            parameter.Value = value is null ? DBNull.Value : value.Value.ToString();
        }

        public override OutboundMailStatus? Parse(object value)
        {
            if (value is DBNull)
            {
                return null;
            }

            return ParseOutboundMailStatus(value);
        }
    }

    private sealed class InboundMailStatusHandler : SqlMapper.TypeHandler<InboundMailStatus>
    {
        public override void SetValue(IDbDataParameter parameter, InboundMailStatus value)
        {
            parameter.Value = value.ToString();
        }

        public override InboundMailStatus Parse(object value)
        {
            return ParseInboundMailStatus(value);
        }
    }

    private sealed class NullableInboundMailStatusHandler : SqlMapper.TypeHandler<InboundMailStatus?>
    {
        public override void SetValue(IDbDataParameter parameter, InboundMailStatus? value)
        {
            parameter.Value = value is null ? DBNull.Value : value.Value.ToString();
        }

        public override InboundMailStatus? Parse(object value)
        {
            if (value is DBNull)
            {
                return null;
            }

            return ParseInboundMailStatus(value);
        }
    }
}

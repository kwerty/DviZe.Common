using Microsoft.Extensions.Logging;
using System;

namespace Kwerty.DviZe.Logging;

public static class LoggerExtensions
{
    extension(ILogger logger)
    {
        public void LogTrace<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Trace, eventId: default, state, exception: null, formatter);
        }

        public void LogDebug<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Debug, eventId: default, state, exception: null, formatter);
        }

        public void LogInformation<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Information, eventId: default, state, exception: null, formatter);
        }

        public void LogWarning<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Warning, eventId: default, state, exception: null, formatter);
        }

        public void LogError<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Error, eventId: default, state, exception: null, formatter);
        }

        public void LogCritical<TState>(TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Critical, eventId: default, state, exception: null, formatter);
        }

        public void LogTrace<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Trace, eventId: default, state, exception, formatter);
        }

        public void LogDebug<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Debug, eventId: default, state, exception, formatter);
        }

        public void LogInformation<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Information, eventId: default, state, exception, formatter);
        }

        public void LogWarning<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Warning, eventId: default, state, exception, formatter);
        }

        public void LogError<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Error, eventId: default, state, exception, formatter);
        }

        public void LogCritical<TState>(TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Critical, eventId: default, state, exception, formatter);
        }
    }
}


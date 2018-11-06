using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace FluffySpoon.AspNet.LetsEncrypt.Azure
{
	class GenericLoggerAdapter<T> : ILogger<T>
	{
		private readonly ILogger logger;

		public GenericLoggerAdapter(ILogger logger)
		{
			this.logger = logger;
		}

		public IDisposable BeginScope<TState>(TState state)
		{
			return logger.BeginScope(state);
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logger.IsEnabled(logLevel);
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			logger.Log(logLevel, eventId, state, exception, formatter);
		}
	}
}

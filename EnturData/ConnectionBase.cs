using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace EnturData
{
	public abstract class ConnectionBase: IDisposable
	{
		protected DbConnection _connection;

		public ConnectionBase(DbConnection connection)
		{
			_connection = connection;
		}

		public virtual DbTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted)
		{
			return _connection.BeginTransaction( isolationLevel );
		}

		public virtual void Open()
		{
			if (_connection != null && _connection.State == ConnectionState.Closed)
			{
				_connection.Open();
			}
		}

		public virtual void Close()
		{
			if (_connection != null && _connection.State == ConnectionState.Open)
			{
				_connection.Close();
			}
		}

		public DbCommand CreateCommand()
		{
			return _connection.CreateCommand();
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose(bool disposing)
		{
			this.Close();

			if (_connection != null)
				_connection.Dispose();
		}
	}
}

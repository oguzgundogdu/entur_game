using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturData
{
	public class PgConnection: ConnectionBase
	{
#if Debug
		public PgConnection() : base( new NpgsqlConnection( Constants.CONNECTION_STRING ) )
		{

		}
#endif

#if Debug_Dev
		public PgConnection() : base( new NpgsqlConnection( Constants.TEST_CONNECTION_STRING ) )
		{

		}
#endif
	}
}

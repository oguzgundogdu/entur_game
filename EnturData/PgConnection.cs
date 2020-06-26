using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturData
{
	public class PgConnection: ConnectionBase
	{
		public PgConnection() : base( new NpgsqlConnection( "server=localhost;Port=5432;Database=entur_game;Userid=postgres;Password=123456;" ) )
		{

		}
	}
}

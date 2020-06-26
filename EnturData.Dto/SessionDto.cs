using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EnturData.Dto
{
	[Table( "t_sessions", Schema = "public" )]
	public class SessionDto
	{
		[Key]
		[Column( "session_id" )]
		public int SessionId { get; set; }

		[Column("user_id")]
		public int UserId { get; set; }

		[Column("has_turn")]
		public char HasTurn { get; set; }
	}
}

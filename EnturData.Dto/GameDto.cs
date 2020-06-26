using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EnturData.Dto
{
	[Table( "t_games", Schema = "public" )]
	public class GameDto
	{
		[Column("game_id")]
		[Key]
		public int GameId { get; set; }

		[Column( "game_name" )]
		public string GameName { get; set; }

		[Column( "status" )]
		public short Status { get; set; }
	}
}

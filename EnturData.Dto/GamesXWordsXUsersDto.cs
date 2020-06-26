using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace EnturData.Dto
{
	[Table( "t_games_x_words_x_users" )]
	public class GamesXWordsXUsersDto
	{
		[Key]
		[Column( "item_id" )]
		public int ItemId { get; set; }

		[Column( "game_id" )]
		public int GameId { get; set; }

		[Column( "word_id" )]
		public int WordId { get; set; }

		[Column( "user_id" )]
		public int? UserId { get; set; }

		[Column( "status" )]
		public short Status { get; set; }
	}
}

using EnturData;
using EnturData.Dto;
using EnturEntity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace EnturBusiness
{
	public class GameManager: IGameManager
	{
		public EnturTransaction Transaction { get; set; }

		public GamesXWordsXUsers PullWord()
		{
			GamesXWordsXUsers obj = null;
			GamesXWordsXUsersDto objDto = Transaction.GameContentRepository.GetWithRawSql( "SELECT * FROM public.t_games_x_words_x_users WHERE user_id IS NULL ORDER BY random() LIMIT 1" ).First();

			if (objDto != null)
			{
				obj = new GamesXWordsXUsers
				{
					GameId = objDto.GameId,
					ItemId = objDto.ItemId,
					UserId = objDto.UserId
				};

				WordDto word = Transaction.WordRepository.GetByID( objDto.WordId );

				if (word != null)
				{
					obj.Word = new Word
					{
						WordId = word.WordId,
						Tur = word.Tur,
						Eng = word.Eng,
						Status = word.Status
					};
				}
			}

			return obj;
		}


		public void UpdateWord(GamesXWordsXUsers gamesXWordsXUsers)
		{
			GamesXWordsXUsersDto gamesX = Transaction.GameContentRepository.GetByID( gamesXWordsXUsers.ItemId );

			gamesX.Status = gamesXWordsXUsers.Status;
			gamesX.UserId = gamesXWordsXUsers.UserId;
		}

		public void StartGame(Game game = null)
		{
			PgConnection connection = new PgConnection();
			connection.Open();

			using (DbCommand cmd = connection.CreateCommand())
			{
				cmd.CommandText = "public.sp_start_game";
				cmd.CommandType = System.Data.CommandType.StoredProcedure;

				cmd.ExecuteScalar();
			}

			connection.Close();
			connection.Dispose();
		}
	}
}

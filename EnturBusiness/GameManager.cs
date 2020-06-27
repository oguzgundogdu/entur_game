using EnturData;
using EnturData.Dto;
using EnturEntity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace EnturBusiness
{
	public class GameManager: IGameManager
	{
		public EnturTransaction Transaction { get; set; }

		public GamesXWordsXUsers PullWord()
		{
			GamesXWordsXUsers obj = null;
			GamesXWordsXUsersDto objDto = Transaction.GameContentRepository.GetWithRawSql( "SELECT * FROM sp_pull_word()" ).First();

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

		public void AddSession(int userId)
		{
			SessionDto sessionDto = new SessionDto();
			sessionDto.UserId = userId;
			sessionDto.HasTurn = 'N';
			Transaction.SessionRepository.Insert( sessionDto );
		}

		public void ClearSessions()
		{
			PgConnection connection = new PgConnection();
			connection.Open();

			using (DbCommand cmd = connection.CreateCommand())
			{
				cmd.CommandText = "public.sp_clear_sessions";
				cmd.CommandType = System.Data.CommandType.StoredProcedure;

				cmd.ExecuteScalar();
			}

			connection.Close();
			connection.Dispose();
		}

		public void ChangeTurn(int userId)
		{
			PgConnection connection = new PgConnection();
			connection.Open();

			using (DbCommand cmd = connection.CreateCommand())
			{
				cmd.CommandText = "public.sp_change_turn";
				cmd.CommandType = System.Data.CommandType.StoredProcedure;

				DbParameter param = cmd.CreateParameter();
				param.ParameterName = "p_user_id";
				param.DbType = System.Data.DbType.Int32;
				param.Value = userId;
				cmd.Parameters.Add( param );

				cmd.ExecuteScalar();
			}

			connection.Close();
			connection.Dispose();
		}

		public void DeleteSession(int userId)
		{
			SessionDto sessionDto = Transaction.SessionRepository.Get( x => x.UserId == userId ).FirstOrDefault();
			Transaction.SessionRepository.Delete( sessionDto );
		}
	}
}

﻿using EnturData;
using EnturData.Dto;
using EnturEntity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
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

			IEnumerable<GamesXWordsXUsersDto> wordList = Transaction.GameContentRepository.GetWithRawSql( "SELECT * FROM sp_pull_word()" );

			if (wordList != null && wordList.Count() > 0)
			{
				GamesXWordsXUsersDto objDto = Transaction.GameContentRepository.GetWithRawSql( "SELECT * FROM sp_pull_word()" ).First();

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
			if (Transaction.SessionRepository.Get( x => x.UserId == userId ).FirstOrDefault() == null)
			{
				SessionDto sessionDto = new SessionDto();
				sessionDto.UserId = userId;
				sessionDto.HasTurn = 'N';
				Transaction.SessionRepository.Insert( sessionDto );
			}
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

		public int GetAnsweredWordCount(int? userId = null, AnsweredWordType answeredWordType = AnsweredWordType.All)
		{
			int count = 0;
			PgConnection connection = new PgConnection();
			connection.Open();

			using (DbCommand cmd = connection.CreateCommand())
			{
				StringBuilder sb = new StringBuilder( "SELECT COUNT(1) FROM t_games_x_words_x_users" );

				if (userId.HasValue && userId.Value > 0)
				{
					sb.AppendFormat( " WHERE user_id = {0}", userId.Value );
				}

				if (answeredWordType != AnsweredWordType.All)
				{
					sb.AppendFormat( " AND status = {0}", (int)answeredWordType );
				}

				cmd.CommandText = sb.ToString();
				cmd.CommandType = System.Data.CommandType.Text;

				object objCount = cmd.ExecuteScalar();

				if (objCount != null)
					count = Convert.ToInt32( objCount );
			}

			connection.Close();
			connection.Dispose();

			return count;
		}
	}

	public enum AnsweredWordType: byte
	{
		Correct = 1,
		Wrong = 2,
		All = 3
	}
}

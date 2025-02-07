﻿using EnturEntity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturBusiness
{
	public interface IGameManager
	{
		EnturTransaction Transaction { get; set; }
		void StartGame(Game game = null);
		GamesXWordsXUsers PullWord();

		void UpdateWord(GamesXWordsXUsers gamesXWordsXUsers);

		void AddSession(int userId);

		void ClearSessions();

		void DeleteSession(int userId);

		void ChangeTurn(int userId);

		int GetAnsweredWordCount(int? userId = null, AnsweredWordType answeredWordType = AnsweredWordType.All);
	}
}

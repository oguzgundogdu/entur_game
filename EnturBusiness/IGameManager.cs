using EnturEntity;
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
	}
}

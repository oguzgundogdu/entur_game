using EnturEntity;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace EnturBusiness
{
	public class GameContext
	{
		private readonly ConcurrentDictionary<int, GamePoint> _users = new ConcurrentDictionary<int, GamePoint>();
		private readonly ConcurrentDictionary<int, IGameManager> _gameManagers = new ConcurrentDictionary<int, IGameManager>();

		private static object _checkStateSync = new object();
		private volatile int _currentUserId;
		private GameContext()
		{


		}

		public int CurrentUserId { get { return _currentUserId; } set { _currentUserId = value; } }

		public GameContent RequestQuestion(int userId, out string message)
		{
			GameContent content = new GameContent();

			if (userId == this.CurrentUserId)
			{
				lock (_checkStateSync)
				{
					if (userId == this.CurrentUserId)
					{
						IGameManager gameManager = _gameManagers[userId];
						GamesXWordsXUsers gamesXWordsXUsers = gameManager.PullWord();
						content.Word = gamesXWordsXUsers;
					}
				}

				message = string.Empty;
			}
			else
			{
				message = $"Waiting for {_users[this.CurrentUserId].User.Username}";
			}

			content.GamePoints = _users.Values.ToList();

			return content;
		}

		public void RespondQuestion(int userId, GamesXWordsXUsers gamesXWordsXUsers)
		{
			IGameManager gameManager = _gameManagers[userId];
			gameManager.UpdateWord( gamesXWordsXUsers );
			gameManager.Transaction.Commit();

			if (gamesXWordsXUsers.Status == 1)
			{
				GamePoint gamePointCurr = _users[userId];
				GamePoint gamePoint = gamePointCurr;
				gamePoint.Point++;

				while (!_users.TryUpdate( userId, gamePoint, gamePointCurr ))
				{
					Thread.Sleep( 5 );
				}
			}

			int id = -1;
			bool isNext = false;

			foreach (var uId in _users.Keys)
			{
				if (isNext && id == -1)
				{
					id = uId;
					break;
				}

				if (uId == userId)
					isNext = true;
			}

			if (id == -1)
			{
				id = _users.Keys.First();
			}


			CurrentUserId = id;
		}

		public static GameContext Current { get; private set; }

		public static void CreateGame(IServiceProvider serviceProvider)
		{
			EnturTransaction trans = new EnturTransaction();
			IUserManager userManager = serviceProvider.GetService<IUserManager>();
			userManager.Transaction = trans;

			IEnumerable<User> users = userManager.Transaction.UserRepository.Get().Select( x => new User
			{
				UserId = x.UserId,
				Username = x.Username
			} );
			Current = new GameContext();

			foreach (User user in users)
			{
				while (!Current._users.ContainsKey( user.UserId ) && !Current._users.TryAdd( user.UserId, new GamePoint
				{
					User = user,
					Point = 0
				} ))
				{
					Thread.Sleep( 5 );
				}

				EnturTransaction transaction = new EnturTransaction();
				IGameManager gameManager = serviceProvider.GetService<IGameManager>();
				gameManager.Transaction = transaction;

				while (!Current._gameManagers.ContainsKey( user.UserId ) && !Current._gameManagers.TryAdd( user.UserId, gameManager ))
				{
					Thread.Sleep( 5 );
				}
			}

			Current.CurrentUserId = Current._users.First().Key;
		}
	}
}

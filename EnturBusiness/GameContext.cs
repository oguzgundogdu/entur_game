using EnturData.Dto;
using EnturEntity;
using EnturEntity.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EnturBusiness
{
	public class GameContext
	{
		private readonly ConcurrentDictionary<int, GamePoint> _users = new ConcurrentDictionary<int, GamePoint>();
		private readonly ConcurrentDictionary<int, IGameManager> _gameManagers = new ConcurrentDictionary<int, IGameManager>();
		private readonly ConcurrentDictionary<int, WebSocket> _webSockets = new ConcurrentDictionary<int, WebSocket>();

		private readonly byte[] _bag = new byte[4096];
		private readonly object _requestQuestionSync = new object();

		private GameContext()
		{


		}

		public GameContent RequestQuestion(int userId)
		{
			GameContent content = new GameContent();

			IGameManager gameManager = _gameManagers[userId];
			GamesXWordsXUsers gamesXWordsXUsers = null;

			lock (_requestQuestionSync)
			{
				gamesXWordsXUsers = gameManager.PullWord();
				gameManager.Transaction.Commit();

				if (gamesXWordsXUsers != null)
				{
					if (!gamesXWordsXUsers.UserId.HasValue || gamesXWordsXUsers.UserId.Value <= 0 || !_users.ContainsKey( gamesXWordsXUsers.UserId.Value ))
					{
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

						gamesXWordsXUsers.UserId = id;


						gameManager.UpdateWord( gamesXWordsXUsers );
						gameManager.Transaction.Commit();
					}

					gamesXWordsXUsers.Username = _users[gamesXWordsXUsers.UserId.Value].User.Username;
					content.Word = gamesXWordsXUsers;
					//message = $"Waiting for {_users[this.CurrentUser.UserId.Value].User.Username}.</br> The word is: {this.CurrentUser.Word.Eng} ({this.CurrentUser.Word.Tur})";
				}
				else
				{
					content.Finished = true;
				}
			}

			content.GamePoints = _users.Values.ToList();

			return content;
		}

		public void RespondQuestion(int userId, GamesXWordsXUsers gamesXWordsXUsers)
		{
			lock (_requestQuestionSync)
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
			}
		}

		public static GameContext Current { get; private set; }

		public static void CreateGame(IServiceProvider serviceProvider)
		{
			IGameManager gameManager = serviceProvider.GetService<IGameManager>();
			createGame( gameManager );
		}

		public static void CreateGame(IGameManager gameManager)
		{
			createGame( gameManager );
		}

		private static void createGame(IGameManager gameManager)
		{
			gameManager.ClearSessions();

			Current = new GameContext();
		}

		public void EnterGame(int userId, IGameManager gameManager, IUserManager userManager)
		{
			EnturTransaction trans = new EnturTransaction();
			gameManager.Transaction = trans;
			userManager.Transaction = trans;

			User user = userManager.GetUserById( userId );
			gameManager.AddSession( user.UserId );
			trans.Commit();

			if (!Current._users.ContainsKey( user.UserId ))
			{
				int point = gameManager.GetAnsweredWordCount( userId, AnsweredWordType.Correct );

				while (!Current._users.ContainsKey( user.UserId ) && !Current._users.TryAdd( user.UserId, new GamePoint
				{
					User = user,
					Point = point
				} ))
				{
					Thread.Sleep( 5 );
				}
			}

			while (!Current._gameManagers.ContainsKey( user.UserId ) && !Current._gameManagers.TryAdd( user.UserId, gameManager ))
			{
				Thread.Sleep( 5 );
			}
		}

		public void ExitGame(int userId)
		{
			IGameManager gameManager = _gameManagers[userId];
			gameManager.DeleteSession( userId );
			gameManager.Transaction.Commit();

			while (_users.ContainsKey( userId ) && !_users.TryRemove( userId, out GamePoint gamePoint ))
			{
				Thread.Sleep( 5 );
			}

			while (_webSockets.ContainsKey( userId ) && !_webSockets.TryRemove( userId, out WebSocket w ))
			{
				Thread.Sleep( 5 );
			}
		}

		public async Task Join(WebSocket wSocket)
		{

			WebSocketReceiveResult result = await wSocket.ReceiveAsync( new ArraySegment<byte>( _bag ), CancellationToken.None );
			string inComingMesage = Encoding.UTF8.GetString( _bag, 0, result.Count );

			if (!string.IsNullOrWhiteSpace( inComingMesage ))
			{
				int userId = Convert.ToInt32( inComingMesage );

				if (_webSockets.ContainsKey( userId ))
				{
					WebSocket s = null;
					while (_webSockets.ContainsKey( userId ) && !_webSockets.TryRemove( userId, out s )) { Thread.Sleep( 5 ); }

					if (s != null)
						await s.CloseAsync( WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None );
				}

				while (!_webSockets.ContainsKey( userId ) && !_webSockets.TryAdd( userId, wSocket )) { Thread.Sleep( 5 ); }

				GameContent content = Current.RequestQuestion( userId );

				SocketResponse response = new SocketResponse();

				response.GameContent = content;
				response.Success = true;

				string strJson = JsonConvert.SerializeObject( response );
				byte[] outGoingMessage = Encoding.UTF8.GetBytes( strJson );
				await wSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );

				if (!content.Finished)
					await Listen( wSocket );
			}

			await wSocket.CloseAsync( result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None );
		}

		private async Task Listen(WebSocket wSocket)
		{
			WebSocketReceiveResult result = null;

			do
			{
				result = await wSocket.ReceiveAsync( new ArraySegment<byte>( _bag ), CancellationToken.None );
				string inComingMesage = Encoding.UTF8.GetString( _bag, 0, result.Count );

				if (string.IsNullOrWhiteSpace( inComingMesage ))
				{
					break;
				}

				if (inComingMesage.StartsWith( "EXIT" ))
				{
					string[] arr = inComingMesage.Split( '|' );
					int userId = Convert.ToInt32( arr[1] );

					IGameManager gameManager = _gameManagers[userId];
					gameManager.DeleteSession( userId );


					while (_gameManagers.ContainsKey( userId ) && !_gameManagers.TryRemove( userId, out IGameManager m ))
					{
						Thread.Sleep( 5 );
					}

					while (_users.ContainsKey( userId ) && !_users.TryRemove( userId, out GamePoint p ))
					{
						Thread.Sleep( 5 );
					}

					while (_webSockets.ContainsKey( userId ) && !_webSockets.TryRemove( userId, out WebSocket w ))
					{
						Thread.Sleep( 5 );
					}

					break;
				}

				GamesXWordsXUsers objComing = JsonConvert.DeserializeObject<GamesXWordsXUsers>( inComingMesage );

				if (objComing == null || !objComing.UserId.HasValue || !_users.ContainsKey( objComing.UserId.Value ))
				{
					break;
				}

				Current.RespondQuestion( objComing.UserId.Value, objComing );

				GameContent content = Current.RequestQuestion( objComing.UserId.Value );

				SocketResponse response = new SocketResponse();
				response.GameContent = content;
				response.Success = true;

				if (response != null && content != null)
				{
					string strJson = JsonConvert.SerializeObject( response );
					byte[] outGoingMessage = Encoding.UTF8.GetBytes( strJson );

					foreach (WebSocket webSocket in _webSockets.Values)
					{
						await webSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );
					}

					if (response.GameContent.Finished)
						break;
				}
			} while (result != null && !result.CloseStatus.HasValue);
		}
	}

}


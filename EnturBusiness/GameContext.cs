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

		private static object _checkStateSync = new object();
		private volatile GamesXWordsXUsers _currentUser;

		private readonly byte[] _bag = new byte[4096];
		private GameContext()
		{


		}

		public GamesXWordsXUsers CurrentUser { get { return _currentUser; } set { _currentUser = value; } }

		public GameContent RequestQuestion(int userId, out string message)
		{
			GameContent content = new GameContent();

			IGameManager gameManager = _gameManagers[userId];
			GamesXWordsXUsers gamesXWordsXUsers = gameManager.PullWord();
			gameManager.Transaction.Commit();

			if (gamesXWordsXUsers != null)
			{
				this.CurrentUser = gamesXWordsXUsers;
				content.Word = gamesXWordsXUsers;
				content.GamePoints = _users.Values.ToList();

				if (!this.CurrentUser.UserId.HasValue || this.CurrentUser.UserId.Value <= 0 || !_users.ContainsKey(this.CurrentUser.UserId.Value))
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

					this.CurrentUser.UserId = id;

					gameManager.UpdateWord( this.CurrentUser );
					gameManager.Transaction.Commit();
				}
			}

			message = $"Waiting for {_users[this.CurrentUser.UserId.Value].User.Username}.</br> The word is: {this.CurrentUser.Word.Eng} ({this.CurrentUser.Word.Tur})";

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
		}

		public static GameContext Current { get; private set; }

		public static void CreateGame(IServiceProvider serviceProvider)
		{

			IGameManager gameManager = serviceProvider.GetService<IGameManager>();

			gameManager.ClearSessions();
			//IEnumerable<User> users = userManager.Transaction.UserRepository.Get().Select( x => new User
			//{
			//	UserId = x.UserId,
			//	Username = x.Username
			//} );
			Current = new GameContext();

			//foreach (User user in users)
			//{
			//	while (!Current._users.ContainsKey( user.UserId ) && !Current._users.TryAdd( user.UserId, new GamePoint
			//	{
			//		User = user,
			//		Point = 0
			//	} ))
			//	{
			//		Thread.Sleep( 5 );
			//	}

			//	EnturTransaction transaction = new EnturTransaction();
			//	IGameManager gameManager = serviceProvider.GetService<IGameManager>();
			//	gameManager.Transaction = transaction;

			//	while (!Current._gameManagers.ContainsKey( user.UserId ) && !Current._gameManagers.TryAdd( user.UserId, gameManager ))
			//	{
			//		Thread.Sleep( 5 );
			//	}
			//}

			//Current.CurrentUserId = Current._users.First().Key;
		}

		public void EnterGame(int userId, IGameManager gameManager, IUserManager userManager)
		{
			EnturTransaction trans = new EnturTransaction();
			gameManager.Transaction = trans;
			userManager.Transaction = trans;

			User user = userManager.GetUserById( userId );
			gameManager.AddSession( user.UserId );
			trans.Commit();

			int point = gameManager.GetAnsweredWordCount( userId, AnsweredWordType.Correct );

			while (!Current._users.ContainsKey( user.UserId ) && !Current._users.TryAdd( user.UserId, new GamePoint
			{
				User = user,
				Point = point
			} ))
			{
				Thread.Sleep( 5 );
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
					while (_webSockets.ContainsKey( userId ) && !_webSockets.TryRemove( userId, out WebSocket s )) { Thread.Sleep( 5 ); }
				}

				while (!_webSockets.ContainsKey( userId ) && !_webSockets.TryAdd( userId, wSocket )) { Thread.Sleep( 5 ); }

				string message;
				GameContent content = Current.RequestQuestion( userId, out message );

				SocketResponse response = new SocketResponse();

				if (content.Word.UserId == userId)
				{
					response.GameContent = content;
				}
				else
				{
					response.GameContent = new GameContent
					{
						GamePoints = content.GamePoints
					};
					response.Message = message;
				}

				response.Success = true;

				string strJson = JsonConvert.SerializeObject( response );
				byte[] outGoingMessage = Encoding.UTF8.GetBytes( strJson );
				await wSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );

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

				SocketResponse response = null;
				SocketResponse contenderResponse = new SocketResponse();

				Current.RespondQuestion( objComing.UserId.Value, objComing );

				string message;
				GameContent contendersContent = Current.RequestQuestion( objComing.UserId.Value, out message );

				if (contendersContent.Word.UserId.Value != objComing.UserId.Value)
				{
					response = new SocketResponse();
					response.GameContent = new GameContent();
					response.GameContent.GamePoints = contendersContent.GamePoints;
					response.Message = message;
					response.Success = true;
				}

				contenderResponse.GameContent = contendersContent;
				contenderResponse.Success = true;

				string strJson = null;
				byte[] outGoingMessage = null;

				if (response != null)
				{
					strJson = JsonConvert.SerializeObject( response );
					outGoingMessage = Encoding.UTF8.GetBytes( strJson );
					await wSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );
				}

				strJson = JsonConvert.SerializeObject( contenderResponse );
				outGoingMessage = Encoding.UTF8.GetBytes( strJson );

				WebSocket contendersSocket = _webSockets[contendersContent.Word.UserId.Value];
				await contendersSocket.SendAsync( new ArraySegment<byte>( outGoingMessage, 0, outGoingMessage.Length ), result.MessageType, result.EndOfMessage, CancellationToken.None );

			} while (result != null && !result.CloseStatus.HasValue);
		}
	}

}


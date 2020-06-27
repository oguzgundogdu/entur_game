using System;
using EnturBusiness;
using EnturEntity;
using EnturEntity.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EnturService.Controllers
{
	[Route( "[controller]" )]
	[ApiController]
	public class GameController: ControllerBase
	{
		[HttpGet]
		[Route( "StartGame" )]
		public JsonResult StartGame([FromServices] IGameManager gameManager, [FromServices] IUserManager userManager)
		{
			ResponseBase response = new ResponseBase();

			try
			{
				gameManager.StartGame();

				response.Message = "Game has been started";
				response.Success = true;
			}
			catch (Exception ex)
			{
				response.Message = ex.Message;
			}

			return new JsonResult( response );
		}

		[HttpPost]
		[Obsolete]
		[Route( "RespondQuestion" )]
		public JsonResult RespondQuestion([FromQuery( Name = "userId" )] string userId, GameContent gameContent)
		{
			ResponseBase response = new ResponseBase();

			try
			{
				if (GameContext.Current != null)
				{
					GameContext.Current.RespondQuestion( Convert.ToInt32( userId ), gameContent.Word );
					response.Success = true;
				}
			}
			catch (Exception ex)
			{
				response.Message = ex.Message;
			}

			return new JsonResult( response );
		}

		[HttpGet]
		[Route( "EnterGame/{userId}" )]
		public JsonResult EnterGame([FromRoute( Name = "userId" )] string userId, [FromServices]IUserManager userManager, [FromServices] IGameManager gameManager)
		{
			ResponseBase response = new ResponseBase();

			try
			{
				GameContext.Current.EnterGame( Convert.ToInt32( userId ), gameManager, userManager );
				response.Success = true;
			}
			catch (Exception ex)
			{
				response.Message = ex.Message;
			}

			return new JsonResult( response );
		}
	}
}
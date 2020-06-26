using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnturBusiness;
using EnturEntity;
using EnturService.Responses;
using Microsoft.AspNetCore.Http;
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
		[Route( "RespondQuestion" )]
		public JsonResult RespondQuestion([FromQuery( Name = "userId" )] string userId, GameContent gameContent)
		{
			ResponseBase response = new ResponseBase();

			try
			{
				if (GameContext.Current != null)
				{
					GameContext.Current.RespondQuestion( Convert.ToInt32(userId), gameContent.Word );
					response.Success = true;
				}
			}
			catch (Exception ex)
			{
				response.Message = ex.Message;
			}

			return new JsonResult( response );
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EnturBusiness;
using EnturEntity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EnturService.Controllers
{
	[Route( "[controller]" )]
	[ApiController]
	public class UserController: ControllerBase
	{
		[HttpGet]
		[Route( "Get" )]
		public IEnumerable<User> Get([FromServices] IUserManager userManager, [FromServices] EnturTransaction transaction)
		{
			userManager.Transaction = transaction;

			return userManager.GetUsers();
		}
	}
}
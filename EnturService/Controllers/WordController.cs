using System;
using System.Collections.Generic;
using EnturBusiness;
using EnturEntity;
using EnturEntity.Responses;
using Microsoft.AspNetCore.Mvc;

namespace EnturService.Controllers
{
	[Route( "[controller]" )]
	[ApiController]
	public class WordController: ControllerBase
	{
		[HttpPost]
		[Route( "SaveWord" )]
		public JsonResult SaveWord(Word word, [FromServices] IWordManager wordManager, [FromServices] EnturTransaction transaction)
		{
			WordResponse response = new WordResponse();

			try
			{
				if (word == null || string.IsNullOrWhiteSpace( word.Eng ) || string.IsNullOrWhiteSpace( word.Tur ))
					return null;

				word.Tur = word.Tur.ToLower();
				word.Eng = word.Eng.ToLower();

				wordManager.Transaction = transaction;
				wordManager.SaveWord( word );

				transaction.Commit();
				response.Message = string.Concat( word.Eng, " saved successfully" );
				response.Success = true;
			}
			catch (Exception ex)
			{
				response.Message = ex.Message;
			}

			return new JsonResult( response );
		}

		[HttpGet]
		[Route( "Get" )]
		public IEnumerable<Word> Get()
		{
			return null;
		}
	}
}
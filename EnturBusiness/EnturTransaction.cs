using EnturData;
using EnturData.Dto;
using EnturEntity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturBusiness
{
	public class EnturTransaction: IEnturTransaction
	{
		EnturDbContext _context = new EnturDbContext();

		public EnturTransaction()
		{
			this.WordRepository = new GenericRepository<WordDto>( _context );
			this.UserRepository = new GenericRepository<UserDto>( _context );
			this.GameRepository = new GenericRepository<GameDto>( _context );
			this.GameContentRepository = new GenericRepository<GamesXWordsXUsersDto>( _context );
		}

		internal GenericRepository<WordDto> WordRepository { get; set; }

		internal GenericRepository<UserDto> UserRepository { get; set; }

		internal GenericRepository<GameDto> GameRepository { get; set; }

		internal GenericRepository<GamesXWordsXUsersDto> GameContentRepository { get; set; }

		public void Commit()
		{
			_context.SaveChanges();
		}
	}
}

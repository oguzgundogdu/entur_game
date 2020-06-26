using EnturData.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EnturData
{
	public class EnturDbContext: DbContext
	{
		public DbSet<UserDto> Users { get; set; }
		public DbSet<WordDto> Words { get; set; }
		public DbSet<GameDto> Games { get; set; }
		public DbSet<GamesXWordsXUsersDto> GameContents { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			if (!optionsBuilder.IsConfigured)
			{
				optionsBuilder.UseNpgsql( "server=localhost;Port=5432;Database=entur_game;Userid=postgres;Password=123456;" );
			}
		}
	}
}

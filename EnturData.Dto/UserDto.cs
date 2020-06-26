using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnturData.Dto
{
	[Table( "t_users", Schema = "public" )]
	public class UserDto
	{
		[Column( "user_id" )]
		[Key]
		public int UserId { get; set; }

		[Column( "user_name" )]
		public string Username { get; set; }
	}
}

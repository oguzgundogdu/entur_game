using EnturEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnturBusiness
{
	public class UserManager: IUserManager
	{
		public EnturTransaction Transaction { get; set; }

		public IEnumerable<User> GetUsers()
		{
			var users = Transaction.UserRepository.Get();

			return users.Select( x => new User
			{
				UserId = x.UserId,
				Username = x.Username
			} );
		}
	}
}

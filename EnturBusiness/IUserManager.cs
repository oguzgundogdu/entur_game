using EnturEntity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturBusiness
{
	public interface IUserManager
	{
		EnturTransaction Transaction{ get; set; }
		IEnumerable<User> GetUsers();
		User GetUserById(int userId);
	}
}

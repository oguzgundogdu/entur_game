using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EnturEntity
{
	[DataContract]
	public class User
	{
		[DataMember]
		public int UserId { get; set; }

		[DataMember]
		public string Username { get; set; }
	}
}

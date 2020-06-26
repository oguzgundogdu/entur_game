using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EnturEntity
{
	[DataContract]
	public class GamesXWordsXUsers
	{
		[DataMember]
		public int ItemId { get; set; }

		[DataMember]
		public int GameId { get; set; }

		[DataMember]
		public Word Word { get; set; }

		[DataMember]
		public int? UserId { get; set; }

		[DataMember]
		public short Status { get; set; }
	}
}

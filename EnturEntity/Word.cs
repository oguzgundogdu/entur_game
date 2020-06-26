using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace EnturEntity
{
	[DataContract]
	public class Word
	{
		[DataMember]
		public int WordId { get; set; }

		[DataMember]
		public string Eng { get; set; }

		[DataMember]
		public string Tur { get; set; }

		[DataMember]
		public short Status { get; set; }
	}
}

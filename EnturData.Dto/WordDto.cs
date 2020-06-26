using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnturData.Dto
{
	[Table( "t_words", Schema = "public" )]
	public class WordDto
	{
		[Key]
		[Column( "word_id" )]
		public int WordId { get; set; }

		[Column( "word_eng" )]
		public string Eng { get; set; }

		[Column( "word_tur" )]
		public string Tur { get; set; }

		[Column( "status" )]
		public short Status { get; set; }
	}
}

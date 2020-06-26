using EnturData.Dto;
using EnturEntity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturBusiness
{
	public class WordManager: IWordManager
	{

		public EnturTransaction Transaction { get; set; }

		public void SaveWord(Word word)
		{
			if (word.WordId <= 0)
			{
				Transaction.WordRepository.Insert( new EnturData.Dto.WordDto
				{
					Tur = word.Tur,
					Eng = word.Eng,
					Status = 1
				} );
			}
			else
			{
				WordDto wordDto = Transaction.WordRepository.GetByID( word.WordId );
				wordDto.Tur = word.Tur;
				wordDto.Eng = word.Eng;
			}
		}
	}
}

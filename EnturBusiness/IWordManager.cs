using EnturEntity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EnturBusiness
{
	public interface IWordManager
	{
		void SaveWord(Word word);

		EnturTransaction Transaction { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	//Kóðin fenginn af https://www.dotnetperls.com/string-isupper-islower
	static class Extensions
	{
		public static bool IsUpper(this string value)
		{
			// Consider string to be uppercase if it has no lowercase letters.
			for (int i = 0; i < value.Length; i++)
			{
				if (char.IsLower(value[i]))
				{
					return false;
				}
			}
			return true; 
		}

		public static bool IsLower(this string value)
		{
			// Consider string to be lowercase if it has no uppercase letters.
			for (int i = 0; i < value.Length; i++)
			{
				if (char.IsUpper(value[i]))
				{
					return false;
				}
			}
			return true;
		}
	}
}

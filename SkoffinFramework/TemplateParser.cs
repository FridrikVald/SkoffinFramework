using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	public static class TemplateParser
	{
		//isRawTemplate er true ef að við erum að eiga við skrá á diski, false ef við viljum generate-a template
		public static string ParseTemplate(string templateType, string templateName, string ClassName, string TableName, string CategoryName, string PropertyName)
		{
			string className = "";
			string tableName = "";
			string propertyName = "";
			string categoryName = "";

			//Special case fyrir t.d. GPU, GPUType, CPU, o.s.frv. Þetta á við um allar töflur sem byrja á fleiri en einum hástaf
			if (ClassName != "" && ClassName.Substring(0, 2).IsUpper())
			{
				className = ClassName.Substring(0, 3).ToLower() + ClassName.Substring(3);
				tableName = TableName.Substring(0, 3).ToLower() + TableName.Substring(3);
			}
			else if(ClassName != "")
			{
				className = ClassName.First().ToString().ToLower() + ClassName.Substring(1);
				tableName = TableName.First().ToString().ToLower() + TableName.Substring(1);
			}

			//Hér stillum við propertyName ef það kom inn eitthvað PropertyName, annars verður það bara tómur strengur
			//Special case fyrir t.d. GPU, GPUType, CPU, o.s.frv. Þetta á við um allar töflur sem byrja á fleiri en einum hástaf
			if (PropertyName != "" && PropertyName.Substring(0, 2).IsUpper())
			{
				propertyName = PropertyName.Substring(0, 3).ToLower() + PropertyName.Substring(3);
			}
			else if (PropertyName != "")
			{
				propertyName = PropertyName.First().ToString().ToLower() + PropertyName.Substring(1);
			}

			//Hér stillum við categoryName ef það kom inn eitthvað CategoryName, annars verður það bara tómur strengur
			if (CategoryName != "")
			{
				categoryName = CategoryName.First().ToString().ToLower() + CategoryName.Substring(1);
			}

			string templateToParse = System.IO.File.ReadAllText("../../Templates/" + templateType + "/" + templateName + "Template.txt");

			//Parse-um, skiptum út orðum og skilum streng
			return templateToParse.
				Replace("CategoryName", CategoryName).
				Replace("categoryName", categoryName).
				Replace("PropertyName", PropertyName).
				Replace("propertyName", propertyName).
				Replace("ClassName", ClassName).
				Replace("className", className).
				Replace("TableName", TableName).
				Replace("tableName", tableName);
		}

		public static List<string> SplitTemplate(string template)
		{
			//Splittum stengnum upp í lista af línum, það er þægilegra fyrir for lykkjunna hér að neðan
			string[] templateArray = template.Split(new[] { "\r\n", "\r", "\n" },StringSplitOptions.None);

			//Þessi færibreyta heldur utan um splittuðu partana af template-inu sem var sent inn í fallið
			List<string> templateParts = new List<string>();

			//Þessi færibreyta heldur utan um þær línur sem við viljum geyma fyrir hvert list item sem er skilað úr þessu falli
			string validLines = "";

			//Förum í gegnum allar línur template-sins
			for (int i = 0; i < templateArray.Length; i++)
			{
				//Ef að línan inniheldur '--PROPERTY--' þá sleppum við henni og getum bætt textanum sem er kominn inn sem templatePart
				if (templateArray.ElementAt(i).Contains("--PROPERTIES--"))
				{
					templateParts.Add(validLines);
					validLines = "";
				}
				//Hér athugum við hvort við séum á línunni á undan eða eftir --PROPERTIES--, ef svo er þá má sleppa því að setja hana inn ef hún er líka tóm
				else if ((i + 1 < templateArray.Length && templateArray.ElementAt(i + 1).Contains("--PROPERTIES--") ||
					i - 1 >= 0 && templateArray.ElementAt(i - 1).Contains("--PROPERTIES--")) &&
					(Regex.IsMatch(templateArray.ElementAt(i), @"^[a-zA-Z0-9\s,]*$")))
				{

				}
				else
					validLines += templateArray.ElementAt(i) + Environment.NewLine;
			}
			//Bætum við lokalínum skráarinnar
			templateParts.Add(validLines);

			return templateParts;
		}
	}
}

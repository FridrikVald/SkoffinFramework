using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	public static class DatabaseMockup
	{
		static string applicationDbContext;
		static string modelSnapshot;
		static string databaseModelsPath;

		static DatabaseMockup()
		{

		}

		//Les skrár úr Skoffin project-inu og geymir þær í minni, aðrir klasar access-a upplýsingarnar í gegnum þennan klasa. Þetta eru allt upplýsingar úr gagnagrunnsskilgreiningum
		public static void InitializeDatabaseMockup(string baseFolderPath)
		{
			applicationDbContext = File.ReadAllText(baseFolderPath + "Data/ApplicationDbContext.cs");
			modelSnapshot = File.ReadAllText(baseFolderPath + "Migrations/ApplicationDbContextModelSnapshot.cs");
			databaseModelsPath = baseFolderPath + "Models/DatabaseModels";
		}

		//Fer í gegnum ApplicationDbContext og nær í öll töflunöfn(í eintölu sem ClassName og í fleirtölu sem TableName) og í hvaða flokka(categories) á að skipta þeim
		public static List<Tuple<string, string, string>> Get_className_tableName_categoryName_List()
		{
			List<Tuple<string, string, string>> className_tableName_categoryName = new List<Tuple<string, string, string>>();

			Regex regex = new Regex("public DbSet<(.*?){ get; set; }");
			var tableNamesRawStrings = regex.Matches(applicationDbContext);
			foreach (Match tableNameRawString in tableNamesRawStrings)
			{
				string ClassName = "";
				string TableName = "";

				//Förum í gegnum alla stafina í strengnum
				bool switchToTableName = false;
				for (int i = 0; i < tableNameRawString.Groups[1].ToString().Length; i++)
				{
					//Setjum inn í ClassName á meðan við höfum ekki rekist á Non Numerical/Alphabetical tákn
					if (Char.IsLetterOrDigit(tableNameRawString.Groups[1].ToString().ElementAt(i)))
					{
						if (switchToTableName)
							TableName += tableNameRawString.Groups[1].ToString().ElementAt(i);
						else
							ClassName += tableNameRawString.Groups[1].ToString().ElementAt(i);
					}
					else
					{
						//Þegar við lendum á fyrsta óvenjulega tákninu, >, þá gefur það okkur til kynna að við séum búin með ClassName og við getum skipt yfir í TableName
						switchToTableName = true;
					}
				}
				className_tableName_categoryName.Add(new Tuple<string, string, string>(ClassName, TableName, FindCategory(ClassName)));
			}
			return className_tableName_categoryName;
		}

		//Fer í gegnum ApplicationDbContextModelSnapshot sem er auto-generated af visual studio, og pikkar út öll property og property types sem eru undir einni töflu
		public static List<Tuple<string, string>> Get_propertyType_propertyName_List(string tableName)
		{
			List<Tuple<string, string>> propertyType_propertyName_List = new List<Tuple<string, string>>();

			string[] lines = modelSnapshot.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines.ElementAt(i).Contains("b.Property<"))
				{
					Regex regex_propertyType = new Regex(@"b.Property<([a-zA-Z0-9]*)");
					var propertyType = regex_propertyType.Match(lines.ElementAt(i)).Groups[1].ToString();

					Regex regex_propertyName = new Regex("(\"(.*?)\")");
					var propertyName = regex_propertyName.Match(lines.ElementAt(i)).Groups[2].ToString();       //TODO: Ég veit ekki alveg af hverju Property nafnið kemur fram í Groups[2] freakar en Groups[1], það er frekar furðulegt að gera regex leitir sem innihalda sviga, gæti haft að gera með '?', þarf að gera test

					propertyType_propertyName_List.Add(new Tuple<string, string>(propertyType, propertyName));
				}
				if (lines.ElementAt(i).Contains("b.ToTable") && lines.ElementAt(i).Contains("b.ToTable(\"" + tableName + "\");"))
				{
					//Ef við komumst hingað þá erum við búin(vonandi) að setja property types og nöfn í currentTableDefinition fyrir innsent tableName
					return propertyType_propertyName_List;
				}
				if (lines.ElementAt(i).Contains("b.ToTable"))
				{
					//Ef við komumst hingað þá fannst taflan ekki og við getum núllað currentTableDefinition
					propertyType_propertyName_List.Clear();
				}
			}
			//Ef við komumst hingað hefur orðið villa
			return propertyType_propertyName_List;
		}

		//Finnum alla Categories
		public static List<string> GetCategories()
		{
			var categoryNames = new List<string>();
			string lastUniqueCategory = "";
			foreach (var element in Get_className_tableName_categoryName_List())
			{
				if (element.Item3 != lastUniqueCategory)
				{
					categoryNames.Add(element.Item3);
					lastUniqueCategory = element.Item3;
				}
			}
			return categoryNames;
		}

		//Finnum alla klasa(töflur) sem eiga heima undir einum Category
		public static List<Tuple<string,string>> Get_className_categoryName_InCategory(string CategoryName)
		{
			var className_categoryName_List = new List<Tuple<string, string>>();
			foreach(var element in Get_className_tableName_categoryName_List())
			{
				if (element.Item3 == CategoryName)
					className_categoryName_List.Add(new Tuple<string, string>(element.Item1, element.Item2));
			}
			return className_categoryName_List;
		}

		//Athugar hvort eitthvað property sé selectable
		public static bool IsSelectable(string CategoryName, string PropertyName)
		{
			//Athugum hvort strengurinn sé nógu langur til að vera vísun í selectable, athugum líka hvort hann inniheldur Id
			if (PropertyName.Length <= 2 || (!PropertyName.Contains("Id")))
				return false;

			//Athugum hvort einhver skrá finnist fyrir property-ið, ef hún finnst ekki þá er þetta property ekki töflunafn og er því ekki selectable
			if (!(File.Exists(databaseModelsPath + CategoryName + "Models/" + PropertyName.Substring(0, PropertyName.Length - 2) + ".cs")))
				return false;

			//Athugum hvort töfluskilgreiningin inniheldur "SelectableEntity", ef svo er þetta property selectable entity
			if (File.ReadAllText(databaseModelsPath + CategoryName + "Models/" + PropertyName.Substring(0, PropertyName.Length - 2) + ".cs").Contains("SelectableEntity"))
				return true;

			return false;
		}

		//Nær í Flags fyrir töfluna ef þau eru til staðar, skilar þeim öllum í einum löngum streng þannig að hægt er að nota .Contains til að athuga hvort eitthvað Flag hafi fundist
		public static string GetFlagsForClassName(string ClassName)
		{
			string[] lines = modelSnapshot.Split(new[] { Environment.NewLine },StringSplitOptions.None);

			for (int i = 0; i < lines.Length; i++)
			{
				if (lines.ElementAt(i).Contains("public DbSet<" + ClassName))
				{
					//Ef við finnum klasanafnið þá getum við leitað að //Flag: í línunni
					if (lines.ElementAt(i).Contains("//Flags:"))
					{
						Regex regex = new Regex("//Flags:[ ][a-zA-Z0-9_ ,]*");

						var c = Regex.Replace(regex.Match(lines.ElementAt(i)).ToString().Substring(8), "[ ,]*", "");

						return Regex.Replace(regex.Match(lines.ElementAt(i)).ToString().Substring(8), "[ ,]*", "");
					}
					break;
				}
			}

			//Ekkert Flag fannst fyrir töfluna
			return "";
		}
		
		//Finnum Category sem að tafla(ClassName) tilheyrir
		private static string FindCategory(string ClassName)
		{
			string[] lines = applicationDbContext.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines.ElementAt(i).Contains("public DbSet<" + ClassName))
				{
					//Ef við finnum klasanafnið þá byrjum við að fara upp línur þangað til við finnum fyrstu línuna sem inniheldur //Category, hún geymir flokkinn sem þessi klasi/tafla tilheyrir
					for (int j = i; j >= 0; j--)
					{
						if (lines.ElementAt(j).Contains("//Category: "))
						{
							Regex regex = new Regex("[ ]*[a-zA-Z0-9_]*");
							return Regex.Replace(regex.Match(lines.ElementAt(j).Substring(13)).ToString(), "[ ]*", "");
						}
					}
					break;
				}
			}
			return "NoCategory";
		}
	}
}

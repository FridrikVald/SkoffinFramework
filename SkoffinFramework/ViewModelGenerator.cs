using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	public static class ViewModelGenerator
	{
		public static void DeleteViewModels(string path)
		{
			//Eyðum möppu
			if (Directory.Exists(path))
				System.IO.Directory.Delete(path, true);
		}

		public static void GenerateViewModels(string path, string autogenWarningText)
		{
			//Eyðum möppu
			DeleteViewModels(path);

			//Búum möppu til upp á nýtt
			System.IO.Directory.CreateDirectory(path);

			foreach (var element in DatabaseMockup.Get_className_tableName_categoryName_List())
			{
				//Náum í alla flags sem eru á tölfunni(ClassName)
				var flags = DatabaseMockup.GetFlagsForClassName(element.Item1);

				//Generate-um ClassNameViewModel file-ana, ef það er eitthvað view notað(Create, Edit eða Details) þá þurfum við ViewModel fyrir það
				if (!(flags.Contains("NoCreate") && flags.Contains("NoEdit") && flags.Contains("NoDetails")))
					GenerateViewModelFile(GenerateViewModelText("ViewModel", "ViewModel", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1, element.Item3, path);

				//Generate-um ClassNameListViewModel file-ana
				if (!(flags.Contains("NoList")))
					GenerateViewModelFile(GenerateViewModelText("ViewModel", "ViewModelList", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1 + "List", element.Item3, path);
			}
		}

		private static void GenerateViewModelFile(string viewModelText, string viewModelName, string CategoryName, string path)
		{
			//Ef að mappan er ekki til þá búum við hana til
			if (!(Directory.Exists(path + CategoryName + "ViewModels")))
				System.IO.Directory.CreateDirectory(path + CategoryName + "ViewModels");

			//Búum til file-inn
			System.IO.File.WriteAllText(path + CategoryName + "ViewModels/" + viewModelName + "ViewModel" + ".cs", viewModelText);
		}

		private static string GenerateViewModelText(string templateType, string templateName, string ClassName, string TableName, string CategoryName, string autogenWarningText)
		{
			//Splittum template-inu í flokka eftir --PROPERTIES-- taginu
			var splitTemplate = TemplateParser.SplitTemplate(TemplateParser.ParseTemplate(templateType, templateName, ClassName, TableName, CategoryName, ""));

			//Náum í lista af properties fyrir töflunafnið
			var properties = DatabaseMockup.Get_propertyType_propertyName_List(TableName);

			//Byrjum á að smíða texta strenginn, setjum fyrst inn viðvörun um að file-inn sé auto generated
			string viewModelText = "//" + autogenWarningText + Environment.NewLine;

			//Setjum inn efri part splittaða template-sins
			viewModelText += splitTemplate.ElementAt(0).ToString();

			foreach (var element in properties)
			{
				//Athugum hvort að porperty type sé selectable, við viljum aðeins generate-a ef við erum að eiga við selectable týpu
				if (DatabaseMockup.IsSelectable(CategoryName, element.Item2))
				{
					viewModelText += TemplateParser.ParseTemplate(templateType, templateType + "selectable", ClassName + "List", TableName, CategoryName, element.Item2.Substring(0, element.Item2.Length - 2));
					viewModelText += Environment.NewLine;
				}
			}

			//Setjum inn lokahluta template-sins
			viewModelText += splitTemplate.ElementAt(1);

			return viewModelText;
		}
	}
}

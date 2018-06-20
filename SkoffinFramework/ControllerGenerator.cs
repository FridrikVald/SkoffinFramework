using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	public static class ControllerGenerator
	{
		public static void DeleteControllers(string path)
		{
			//Eyðum möppu
			if (Directory.Exists(path))
				System.IO.Directory.Delete(path, true);
		}

		public static void GenerateControllers(string path, string autogenWarningText)
		{
			//Eyðum möppu
			DeleteControllers(path);

			//Búum möppu til upp á nýtt
			System.IO.Directory.CreateDirectory(path);

			foreach (var category in DatabaseMockup.GetCategories())
			{
				//Generate-um einn controller fyrir hvern category sem finnst
				GenerateControllerFile(GenerateControllerText(category, autogenWarningText), category, path);
			}
		}

		private static void GenerateControllerFile(string controllerText, string controllerName, string path)
		{
			//Búum til file-inn
			System.IO.File.WriteAllText(path + "CRUD" + controllerName + "Controller" + ".cs", controllerText);
		}

		//Býr til texta fyrir heilan controller
		private static string GenerateControllerText(string CategoryName, string autogenWarningText)
		{
			//Splittum template-inu í flokka eftir --PROPERTIES-- taginu
			var splitTemplate = TemplateParser.SplitTemplate(TemplateParser.ParseTemplate("Controller", "Controller", "", "", CategoryName, ""));

			//Byrjum á að smíða texta strenginn, setjum fyrst inn viðvörun um að file-inn sé auto generated
			string controllerText = "//" + autogenWarningText + Environment.NewLine;

			//Setjum inn efri hluta controller skilgreiningar
			controllerText += splitTemplate.ElementAt(0) + Environment.NewLine + Environment.NewLine + Environment.NewLine;

			//Náum í lista af öllum klösum(töflum) sem eiga heima innan CategoryName flokksins
			var classesInCategory = DatabaseMockup.Get_className_categoryName_InCategory(CategoryName);

			//Setjum inn allar controller functions
			for (int i = 0; i < DatabaseMockup.Get_className_categoryName_InCategory(CategoryName).Count; i++)
			{
				controllerText += GenerateControllerTextForSingleClass("Controller", "Controller", classesInCategory.ElementAt(i).Item1, classesInCategory.ElementAt(i).Item2, CategoryName);

				//Setjum inn newlines á milli klasa ef við erum ekki á síðasta klasanum
				if (i != DatabaseMockup.Get_className_categoryName_InCategory(CategoryName).Count - 1)
					controllerText += Environment.NewLine + Environment.NewLine;
			}

			//Setjum inn neðri hluta controller-sins, það ættu bara að vera tveir }
			controllerText += splitTemplate.ElementAt(1);

			return controllerText;
		}

		//Býr til texta fyrir allar functions sem einn klasi þarf innan controllers
		private static string GenerateControllerTextForSingleClass(string templateType, string templateName, string ClassName, string TableName, string CategoryName)
		{
			//Náum í alla flags sem eru á tölfunni(ClassName)
			var flags = DatabaseMockup.GetFlagsForClassName(ClassName);

			//Byrjum á að smíða loka template strenginn,
			string controllerString = "";

			//Setjum inn smá comment á milli tafla(klasa) til að það verði skýrara að skoða controller-inn. 
			controllerString += "\t\t/*\r\n";
			controllerString += "\t\t\t" + ClassName + " functions\r\n";
			controllerString += "\t\t*/\r\n";

			//Setjum inn List functions
			if (!(flags.Contains("NoList")))
				controllerString += GetPropertiesTextForControllerFunctionString(templateType, templateName + "List", ClassName, TableName, CategoryName);

			//Setjum inn Create functions
			if (!(flags.Contains("NoCreate")))
				controllerString += GetPropertiesTextForControllerFunctionString(templateType, templateName + "Create", ClassName, TableName, CategoryName);

			//Setjum inn Edit functions
			if (!(flags.Contains("NoEdit")))
				controllerString += GetPropertiesTextForControllerFunctionString(templateType, templateName + "Edit", ClassName, TableName, CategoryName);

			//Setjum inn Details function
			if (!(flags.Contains("NoDetails")))
				controllerString += GetPropertiesTextForControllerFunctionString(templateType, templateName + "Details", ClassName, TableName, CategoryName);

			//Setjum inn Delete function
			if (!(flags.Contains("NoDelete")))
				controllerString += TemplateParser.ParseTemplate(templateType, templateName + "Delete", ClassName, TableName, CategoryName, "") + Environment.NewLine + Environment.NewLine;

			//Ef að engin functions fyrir þessa töflu áttu að fara í controller-inn, þá setjum við inn eitt línubil til að jafna útlitið á generated controller-num
			if (flags.Contains("NoList") && flags.Contains("NoCreate") && flags.Contains("NoEdit") && flags.Contains("NoDetails") && flags.Contains("NoDelete"))
			{
				controllerString += Environment.NewLine;
			}

			return controllerString;
		}

		//Býr til streng sem að inniheldur textann fyrir --PROPERTIES-- innan controller function template-ana
		private static string GetPropertiesTextForControllerFunctionString(string templateType, string templateName, string ClassName, string TableName, string CategoryName)
		{
			string propertiesControllerString = "";

			//Splittum template-inu í flokka eftir --PROPERTIES-- taginu
			var splitTemplate = TemplateParser.SplitTemplate(TemplateParser.ParseTemplate(templateType, templateName, ClassName, TableName, CategoryName, ""));

			//Náum í lista af properties fyrir töflunafnið
			var properties = DatabaseMockup.Get_propertyType_propertyName_List(TableName);

			//Setjum inn efsta part splittaða template-sins
			propertiesControllerString += splitTemplate.ElementAt(0).ToString();

			foreach (var element in properties)
			{
				//Athugum hvort að porperty type sé selectable, við viljum aðeins generate-a ef við erum að eiga selectable týpu
				if (DatabaseMockup.IsSelectable(CategoryName, element.Item2))
				{
					propertiesControllerString += TemplateParser.ParseTemplate(templateType, templateType + "Selectable", ClassName, TableName, CategoryName, element.Item2.Substring(0, element.Item2.Length - 2));
					propertiesControllerString += Environment.NewLine;
				}
			}

			//Við athugum hvort að eitthvað hafi komið inn í --PROPERTIES--, annaðhvort í foreach lykkjunni hér að ofan eða í Template-inu, ef að síðasta táknið í propertiesControllerString er { þá kom 
			//ekkert inn(það er -3 vegna þess að síðustu táknin eru: '{' '\r' '\n')
			if (!(propertiesControllerString.ElementAt(propertiesControllerString.Length - 3) == '{'))
			{
				//Ef eitthvað kom inn þá þurfum við að taka út síðustu kommuna(sem ætti að vera alveg síðasta táknið í strengnun)
				propertiesControllerString = propertiesControllerString.Substring(0, propertiesControllerString.Length - 3) + Environment.NewLine;
			}

			//Setjum inn lokahluta template-sins
			propertiesControllerString += splitTemplate.ElementAt(1) + Environment.NewLine;

			return propertiesControllerString;
		}
	}
}

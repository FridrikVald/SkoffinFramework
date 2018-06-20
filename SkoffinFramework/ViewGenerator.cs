using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkoffinFramework
{
	public static class ViewGenerator
	{
		public static void DeleteViews(string path)
		{
			//Eyðum möppu
			if (Directory.Exists(path))
				System.IO.Directory.Delete(path, true);
		}

		public static void GenerateViews(string path, string autogenWarningText)
		{
			//Eyðum möppu
			DeleteViews(path);

			//Búum möppu til upp á nýtt
			System.IO.Directory.CreateDirectory(path);

			foreach (var element in DatabaseMockup.Get_className_tableName_categoryName_List())
			{
				//Generate-um Create view
				if (!(DatabaseMockup.GetFlagsForClassName(element.Item1).Contains("NoCreate")))
					GenerateViewFile(GenerateViewText("CreateEdit", "CreateView", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1, "Create" + element.Item1, element.Item3, path);

				//Generate-um Edit view 
				if (!(DatabaseMockup.GetFlagsForClassName(element.Item1).Contains("NoEdit")))
					GenerateViewFile(GenerateViewText("CreateEdit", "EditView", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1, "Edit" + element.Item1, element.Item3, path);

				//Generate-um Details view
				if (!(DatabaseMockup.GetFlagsForClassName(element.Item1).Contains("NoDetails")))
					GenerateViewFile(GenerateViewText("Details", "DetailsView", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1, "Details" + element.Item1, element.Item3, path);
				
				//Generate-um List view
				if (!(DatabaseMockup.GetFlagsForClassName(element.Item1).Contains("NoList")))
					GenerateViewFile(GenerateViewText("List", "ListView", element.Item1, element.Item2, element.Item3, autogenWarningText), element.Item1, element.Item2, element.Item3, path);
			}
		}

		private static void GenerateViewFile(string viewText, string viewFolderName, string viewName, string CategoryName, string path)
		{
			//Ef að mappan er ekki til þá búum við hana til
			if(!(Directory.Exists(path + CategoryName + "/" + viewFolderName)))
				System.IO.Directory.CreateDirectory(path + CategoryName + "/" + viewFolderName);

			//Búum til file-inn
			System.IO.File.WriteAllText(path + CategoryName + "/" + viewFolderName + "/" + viewName + ".cshtml", viewText);
		}

		private static string GenerateViewText(string templateType, string templateName, string ClassName, string TableName, string CategoryName, string autogenWarningText)
		{
			//Splittum template-inu í flokka eftir --PROPERTIES-- taginu
			var splitTemplate = TemplateParser.SplitTemplate(TemplateParser.ParseTemplate("Views/" + templateType, templateName, ClassName, TableName, CategoryName, ""));

			//Náum í lista af properties fyrir töflunafnið
			var properties = DatabaseMockup.Get_propertyType_propertyName_List(TableName);

			//Byrjum á að smíða texta strenginn, setjum fyrst inn viðvörun um að file-inn sé auto generated
			string viewText = "@* " + autogenWarningText + " *@" + Environment.NewLine;

			//Síðan setjum við efsta part splittaða template-sins inn
			viewText += splitTemplate.ElementAt(0).ToString();

			//Útbúum færibreytu sem segir okkur til um hvort við séum að fara í gegnum for lykkjuna tvisvar sinnum, það þarf að gera það fyrir List view-in
			int k = 0;
			for (int i = 0; i < properties.Count; i++)
			{
				if(templateType == "CreateEdit")
				{
					//Special case fyrir selectable, það á að vera represent-að á sérstakan hátt, ekki sem int
					if (DatabaseMockup.IsSelectable(CategoryName, properties.ElementAt(i).Item2))
					{
						//Trimmum Id af endanum á property ef við erum að eiga við selectable, Id er sett inn í template-inu fyrir selectable
						string propertyTrimmed = properties.ElementAt(i).Item2.Substring(0, properties.ElementAt(i).Item2.Length - 2);

						viewText += TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Selectable", ClassName, TableName, CategoryName, propertyTrimmed);
					}
					//Special case fyrir boolean
					else if (properties.ElementAt(i).Item1 == "bool")
					{
						viewText += TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Bool", ClassName, TableName, CategoryName, properties.ElementAt(i).Item2);
					}
					//Special case fyrir primary Id, sem er ekki höndlað eins og önnur editable int og er partur af Create og Edit views(sleppt í Create og hafður hidden í Edit)
					else if (properties.ElementAt(i).Item2 != "Id")
					{
						//Ef við erum ekki að deal-a við special case, eins og Id, þá getum við lesið template-ið fyrir property-ið beint úr CreateEdittype template-inu
						viewText += TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Type", ClassName, TableName, CategoryName, properties.ElementAt(i).Item2);
					}

					//Setjum inn newline á milli properties á meðan við erum ekki á primaryKey, þetta er til þess að losna við auka línubil sem myndast annars í Create og Edit fyrir Id-ið, það er ekki höndlað sem property hér heldur tilgreint í ViewTemplate-unum sjálfum
					if (properties.ElementAt(i).Item2 != "Id")
						viewText += Environment.NewLine;
				}
				else if (templateType == "Details")
				{
					viewText += TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Type", ClassName, TableName, CategoryName, properties.ElementAt(i).Item2);

					//Setjum inn newline á milli properties
					viewText += Environment.NewLine;
				}
				else if (templateType == "List")
				{
					var splitListPropertyTypeTemplate = new List<string>();

					//Special case fyrir selectable, það á að vera represent-að á sérstakan hátt, ekki sem int
					if (DatabaseMockup.IsSelectable(CategoryName, properties.ElementAt(i).Item2))
					{
						//Trimmum Id af endanum á property ef við erum að eiga við selectable, Id er sett inn í template-inu fyrir selectable
						string propertyTrimmed = properties.ElementAt(i).Item2.Substring(0, properties.ElementAt(i).Item2.Length - 2);

						//Splittum Template file-um fyrir ListView þar sem við erum bæði með fyrir ofan og neðan töflu í þeim
						splitListPropertyTypeTemplate = TemplateParser.SplitTemplate(	TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Selectable", 
																						ClassName, TableName, CategoryName, propertyTrimmed));
					}
					else
					{
						//Splittum Template file-um fyrir ListView þar sem við erum bæði með fyrir ofan og neðan töflu í þeim
						splitListPropertyTypeTemplate = TemplateParser.SplitTemplate(TemplateParser.ParseTemplate("Views/" + templateType, templateType + "Type",
																				ClassName, TableName, CategoryName, properties.ElementAt(i).Item2));
					}

					if(properties.ElementAt(i).Item2 == "Id")
					{

					}

					//Setjum annaðhvort efri eða neðri part property-sins inn, fer eftir því hvort við erum á first eða second pass í for lykkjunni
					viewText += splitListPropertyTypeTemplate.ElementAt(k).ToString();
				}

				//Ef við erum að eiga við lista og erum komin á enda properties þá þurfum við að gera k að 1 og fara aftur í gegnum properties; það þarf að setja inn neðri helming list-view-sins
				if (templateType == "List" && i == properties.Count - 1 && k == 0)
				{
					i = -1;		//
					k = 1;
					viewText += splitTemplate.ElementAt(1);     //Bætum við miðju úr töflu
				}
			}

			//Setjum inn lokahluta template-sins, það er á mismunandi stað eftir því hvort verið er að eiga við lista eða Edit/Create views
			if (templateType == "List")
				viewText += splitTemplate.ElementAt(2);
			else
				viewText += splitTemplate.ElementAt(1);

			return viewText;
		}
	}
}

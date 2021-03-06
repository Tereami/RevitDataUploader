#region License
/*Данный код опубликован под лицензией Creative Commons Attribution-ShareAlike.
Разрешено использовать, распространять, изменять и брать данный код за основу для производных в коммерческих и
некоммерческих целях, при условии указания авторства и если производные лицензируются на тех же условиях.
Код поставляется "как есть". Автор не несет ответственности за возможные последствия использования.
Зуев Александр, 2021, все права защищены.
This code is listed under the Creative Commons Attribution-ShareAlike license.
You may use, redistribute, remix, tweak, and build upon this work non-commercially and commercially,
as long as you credit the author by linking back and license your new creations under the same terms.
This code is provided 'as is'. Author disclaims any implied warranty.
Zuev Aleksandr, 2021, all rigths reserved.*/
#endregion
#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
#endregion

namespace RevitDataUploader
{
    public static class DocumentGetter
    {
        public static IEnumerable<Element> GetConstructions(this Document doc, View main3dView)
        {
            List<BuiltInCategory> cats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Floors,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_StructConnections,
                BuiltInCategory.OST_Roofs
            };
            ElementMulticategoryFilter constrsFilter =
                new ElementMulticategoryFilter(cats);

            List<Element> elems = new FilteredElementCollector(doc, main3dView.Id)
                .WhereElementIsNotElementType()
                .WherePasses(constrsFilter)
                .ToList();

            List<FamilyInstance> genericModels = new FilteredElementCollector(doc, doc.GetMain3dView().Id)
                .WhereElementIsNotElementType()
                .OfClass(typeof(FamilyInstance))
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .Cast<FamilyInstance>()
                .Where(i => Configuration.genericModelPrefixes.Contains(i.Symbol.FamilyName.Substring(0, 3)))
                .ToList();

            elems.AddRange(genericModels);

            return elems;
        }

        public static IEnumerable<Element> GetRebars(this Document doc, View main3dView)
        {
            IEnumerable<Element> rebars0 = new FilteredElementCollector(doc, main3dView.Id)
                .WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_Rebar);

            List<Element> rebars = new List<Element>();

            foreach (Element e in rebars0)
            {
                if (e is FamilyInstance)
                {
                    FamilyInstance fi = e as FamilyInstance;
                    string familyName = fi.Symbol.FamilyName;
                    if (familyName.StartsWith("220")) //игнорировать контейнеры закладных деталей
                        continue;

                    if (familyName.StartsWith("266")) //игнорировать контейнеры арм каркасов
                        continue;
                }
                rebars.Add(e);
            }
            return rebars;
        }

        public static string GetTitleWithoutExtension(this Document doc)
        {
            string docTitle = doc.Title;
            if (docTitle.EndsWith(".rvt"))
                docTitle = docTitle.Substring(0, docTitle.Length - 4);

            if (docTitle.Contains("_"))
            {
                string[] titleArray = docTitle.Split('_');
                int lengthMinusOne = titleArray.Length - 1;
                string title2 = "";
                for (int i = 0; i < lengthMinusOne; i++)
                {
                    title2 += titleArray[i];
                }
                return title2;
            }
            else
            {
                return docTitle;
            }
        }

        public static View3D GetMain3dView(this Document doc)
        {
            List<View3D> views = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(i => i.Name == "RevitDataUploader")
                .ToList();
            if (views.Count == 0)
                throw new Exception("Нет 3D вида RevitDataUploader");

            View3D view = views.First();

            if (view.DetailLevel != ViewDetailLevel.Fine)
                throw new Exception("Установите Высокую детализацию для вида RevitDataUploader");


            return view;
        }
    }
}

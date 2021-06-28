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
    public class CustomParameterData
    {
        public CustomParameter customParam;
        public View3D View;
        public IEnumerable<Element> Elements;

        public static List<CustomParameterData> GetCustomParamsData(Document doc)
        {
            List<CustomParameterData> data = new List<CustomParameterData>();

            List<View3D> views = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .Where(i => i.Name.StartsWith("RevitDataUploader_") && i.Name.Contains("="))
                .ToList();

            if(views.Count == 0)
                return data;

            foreach(View3D view in views)
            {
                if (view.DetailLevel != ViewDetailLevel.Fine)
                    throw new Exception("Установите Высокую детализацию для вида " + view.Name);

                CustomParameterData customdata = new CustomParameterData();
                customdata.View = view;

                string splitName = view.Name.Split('_').Last();
                string[] splitParam = splitName.Split('=');

                customdata.customParam = new CustomParameter( splitParam[0], splitParam[1]);

                customdata.Elements = new FilteredElementCollector(doc, view.Id)
                    .WhereElementIsNotElementType();
                
                data.Add(customdata);
            }

            return data;
        }
    }
}

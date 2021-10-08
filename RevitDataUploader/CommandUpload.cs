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
using Newtonsoft.Json;
#endregion

namespace RevitDataUploader
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CommandUpload : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document mainDoc = commandData.Application.ActiveUIDocument.Document;

            List<ElementMaterialInfo> elemMaterials = 
                RevitDataUploader.Connector.GetStructureData(mainDoc);


            System.Windows.Forms.SaveFileDialog dialog = 
                new System.Windows.Forms.SaveFileDialog();
            dialog.FileName = mainDoc.Title + ".json";
            dialog.Filter = "JSON files|*.json";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return Result.Cancelled;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;


            int elementsCount = 0;
            string folder = System.IO.Path.GetDirectoryName(dialog.FileName);
            for (int i = 0; i < elemMaterials.Count; i++)
            {
                ElementMaterialInfo emi = elemMaterials[i];
                string docTitle = emi.fileName;
                string filename = System.IO.Path.Combine(folder, docTitle + "_" + emi.uniqueId + ".json");

                int filesCounter = 1;
                while (System.IO.File.Exists(filename))
                {
                    filename = System.IO.Path.Combine(folder, docTitle + "_" + emi.uniqueId + "_" + filesCounter + ".json");
                    filesCounter++;
                }

                using (StreamWriter writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, emi);
                    elementsCount++;
                }
            }

            TaskDialog.Show("Info", "Выгружено элементов: " + elementsCount.ToString());
            return Result.Succeeded;
        }
    }
}

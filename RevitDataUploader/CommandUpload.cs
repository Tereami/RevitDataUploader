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
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CommandUpload : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Dictionary<int, MaterialInfo> materialsBase = MaterialInfo.GetAllMaterials(doc);

            View3D mainView = doc.GetMain3dView();

            List<CustomParameterData> customParamsData = CustomParameterData.GetCustomParamsData(doc);

            Dictionary<int, ElementInfo> elementsBase = new Dictionary<int, ElementInfo>();

            IEnumerable<Element> constructions = doc.GetConstructions();
            foreach (Element constr in constructions)
            {
                ElementInfo einfo = new ElementInfo(constr);
                if(!einfo.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid element " + constr.Id.IntegerValue.ToString());
                    continue;
                }

                elementsBase.Add(constr.Id.IntegerValue, einfo);
            }

            List<ElementMaterialInfo> elemMaterials = new List<ElementMaterialInfo>();

            foreach(var kvp in elementsBase)
            {
                ElementInfo einfo = kvp.Value;

                if (einfo.ElementType == ElementGroup.Rebar)
                {
                    ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                    elemMaterial.ApplyRebar();
                    elemMaterials.Add(elemMaterial);
                }
                else if (einfo.ElementType is ElementGroup.Metal)
                {
                    ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                    elemMaterial.ApplyMetal();
                    elemMaterials.Add(elemMaterial);
                }
                else
                {
                    List<ElementId> materialIds = einfo.RevitElement.GetMaterialIds(false).ToList();

                    foreach (ElementId matid in materialIds)
                    {
                        ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                        int matidInt = matid.IntegerValue;
                        if (!materialsBase.ContainsKey(matidInt))
                        {
                            System.Diagnostics.Debug.WriteLine("No material in base: " + matid);
                            continue;
                        }
                        MaterialInfo matinfo = materialsBase[matidInt];
                        elemMaterial.ApplyMaterial(matinfo);
                        elemMaterials.Add(elemMaterial);
                    }
                }
            }

            System.Xml.Serialization.XmlSerializer serializer = 
                new System.Xml.Serialization.XmlSerializer(typeof(List<ElementMaterialInfo>));

            string xmlFilename = DateTime.Now.ToString().Replace(':', ' ') + ".xml";

            using (StreamWriter writer = new StreamWriter(@"C:\revitupload\" + xmlFilename))
            {
                serializer.Serialize(writer, elemMaterials);
            }

            TaskDialog.Show("Info", "Выгружено элементов: " + elemMaterials.Count.ToString());
            return Result.Succeeded;
        }
    }
}

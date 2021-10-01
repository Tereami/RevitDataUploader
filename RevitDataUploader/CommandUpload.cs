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
            Document doc = commandData.Application.ActiveUIDocument.Document;

            Dictionary<int, MaterialInfo> materialsBase = MaterialInfo.GetAllMaterials(doc);

            View3D mainView = doc.GetMain3dView();

            Dictionary<int, ElementInfo> elementsBase = new Dictionary<int, ElementInfo>();

            IEnumerable<Element> constructions = doc.GetConstructions();
            foreach (Element constr in constructions)
            {
                ElementInfo einfo = new ElementInfo(constr);
                if (!einfo.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid element " + constr.Id.IntegerValue.ToString());
                    continue;
                }

                elementsBase.Add(constr.Id.IntegerValue, einfo);
            }

            IEnumerable<Element> rebars = doc.GetRebars();
            foreach (Element rebar in rebars)
            {
                ElementInfo einfo = new ElementInfo(rebar);
                if (!einfo.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine("Invalid element " + rebar.Id.IntegerValue.ToString());
                    continue;
                }

                elementsBase.Add(rebar.Id.IntegerValue, einfo);
            }

            List<CustomParameterData> customParamsData = CustomParameterData.GetCustomParamsData(doc);
            foreach (CustomParameterData cpdata in customParamsData)
            {
                ParameterInfo cp = cpdata.customParam;
                foreach (Element elem in cpdata.Elements)
                {
                    int elemId = elem.Id.IntegerValue;
                    if (elementsBase.ContainsKey(elemId))
                    {
                        elementsBase[elemId].CustomParameters.Add(cp);
                    }
                }
            }

            List<ElementMaterialInfo> elemMaterials = new List<ElementMaterialInfo>();

            foreach (var kvp in elementsBase)
            {
                ElementInfo einfo = kvp.Value;

                if (einfo.Group == ElementGroup.Rebar)
                {
                    ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                    elemMaterial.ApplyRebar(materialsBase);
                    elemMaterials.Add(elemMaterial);
                }
                else if (einfo.Group is ElementGroup.Metal)
                {
                    ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                    elemMaterial.ApplyMetal();
                    elemMaterials.Add(elemMaterial);
                }
                else
                {
                    Dictionary<bool, List<ElementId>> matids = new Dictionary<bool, List<ElementId>>();
                    List<ElementId> materialIdsNoPaint = einfo.RevitElement.GetMaterialIds(false).ToList();
                    List<ElementId> materialIdsPaint = einfo.RevitElement.GetMaterialIds(true).ToList();
                    matids.Add(false, materialIdsNoPaint);
                    matids.Add(true, materialIdsPaint);

                    foreach(var mat in matids)
                    {
                        List<ElementId> curMatIds = mat.Value;
                        foreach (ElementId matid in curMatIds)
                        {
                            ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                            MaterialInfo matinfo = materialsBase[matid.IntegerValue];
                            if(matinfo.CalcType == MaterialCalcType.None)
                            {
                                continue;
                            }
                            matinfo.IsPaint = mat.Key;
                            elemMaterial.ApplyMaterial(matinfo);
                            elemMaterials.Add(elemMaterial);
                        }
                    }
                }
            }

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.FileName = doc.Title + "_" + DateTime.Now.ToString().Replace(':', ' ') + ".json";
            dialog.Filter = "JSON files|*.json";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return Result.Cancelled;

            //List<ItemInfo> infos = elemMaterials.Select(i => new ItemInfo(i)).ToList();

            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            int elementsCount = elemMaterials.Count;
            string folder = System.IO.Path.GetDirectoryName(dialog.FileName);
            for(int i = 0; i < elemMaterials.Count; i++)
            {
                ElementMaterialInfo emi = elemMaterials[i];
                ItemInfo ii = new ItemInfo(emi);
                ii.counter = i;
                ii.totalElements = elementsCount;

                string filename = System.IO.Path.Combine(folder, doc.Title + "_" + emi.ElemInfo.RevitUniqueElementId + ".json");

                int filesCounter = 1;
                while(System.IO.File.Exists(filename))
                {
                    filename = System.IO.Path.Combine(folder, doc.Title + "_" + emi.ElemInfo.RevitUniqueElementId + "_" + filesCounter + ".json");
                    filesCounter++;
                }
                
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, ii);
                }
            }

            TaskDialog.Show("Info", "Выгружено элементов: " + elementsCount.ToString());
            return Result.Succeeded;
        }
    }
}

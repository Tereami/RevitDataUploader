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
            List<Document> docs = new List<Document> { mainDoc };

            List<RevitLinkInstance> linkInsts = new FilteredElementCollector(mainDoc)
                .WhereElementIsNotElementType()
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .ToList();

            Dictionary<string, List<string>> linkInstancesNames = new Dictionary<string, List<string>>();
            if (linkInsts.Count > 0)
            {
                Dictionary<string, List<RevitLinkInstance>> linkDocNames =
                    new Dictionary<string, List<RevitLinkInstance>>();

                foreach (RevitLinkInstance rli in linkInsts)
                {
                    string linkDocName = rli.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString();
                    if (linkDocName.EndsWith(".rvt"))
                        linkDocName = linkDocName.Replace(".rvt", "");

                    if (linkDocNames.ContainsKey(linkDocName))
                        linkDocNames[linkDocName].Add(rli);
                    else
                        linkDocNames[linkDocName] = new List<RevitLinkInstance> { rli };
                }


                FormSelectLinks formLinks = new FormSelectLinks(linkDocNames.Keys.ToList());
                if (formLinks.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return Result.Cancelled;

                foreach (string docName in formLinks.selectedDocs)
                {
                    List<RevitLinkInstance> curLinks = linkDocNames[docName];
                    Document linkDoc = curLinks[0].GetLinkDocument();
                    docs.Add(linkDoc);

                    if (curLinks.Count == 1) continue;

                    foreach (RevitLinkInstance rli in curLinks)
                    {
                        string linkInstanceName = rli.get_Parameter(BuiltInParameter.RVT_LINK_INSTANCE_NAME).AsString();
                        if (linkInstancesNames.ContainsKey(docName))
                            linkInstancesNames[docName].Add(linkInstanceName);
                        else
                            linkInstancesNames.Add(docName, new List<string> { linkInstanceName });
                    }
                }
            }

            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.FileName = mainDoc.Title + ".json";
            dialog.Filter = "JSON files|*.json";

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return Result.Cancelled;

            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            List<ElementMaterialInfo> elemMaterials = new List<ElementMaterialInfo>();

            foreach (Document doc in docs)
            {
                View main3dView = doc.GetMain3dView();
                Dictionary<int, MaterialInfo> materialsBase = MaterialInfo.GetAllMaterials(doc);

                Dictionary<int, ElementInfo> elementsBase = new Dictionary<int, ElementInfo>();

                IEnumerable<Element> constructions = doc.GetConstructions(main3dView);
                foreach (Element constr in constructions)
                {
                    ElementInfo ei = new ElementInfo(constr);
                    elementsBase.Add(constr.Id.IntegerValue, ei);
                }

                IEnumerable<Element> rebars = doc.GetRebars(main3dView);
                foreach (Element rebar in rebars)
                {
                    ElementInfo einfo = new ElementInfo(rebar);
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


                List<ElementInfo> infos = new List<ElementInfo>();
                string docTitle = doc.Title;
                foreach (var kvp in elementsBase)
                {
                    ElementInfo ei = kvp.Value;
                    int elemId = ei.RevitElement.Id.IntegerValue;
                    if (linkInstancesNames.ContainsKey(docTitle))
                    {
                        List<string> blockNames = linkInstancesNames[docTitle];
                        foreach (string blockName in blockNames)
                        {
                            ElementInfo cloneEi = (ElementInfo)ei.Clone();
                            ParameterInfo blockPi = new ParameterInfo(Configuration.BlockParamName, "Блок №" + blockName);
                            cloneEi.CustomParameters = ei.CustomParameters.ToList();
                            cloneEi.CustomParameters.Add(blockPi);
                            cloneEi.Mark += blockName;
                            infos.Add(cloneEi);
                        }
                    }
                    else
                    {
                        infos.Add(ei);
                    }
                }


                foreach (ElementInfo einfo in infos)
                {
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

                        foreach (var mat in matids)
                        {
                            List<ElementId> curMatIds = mat.Value;
                            foreach (ElementId matid in curMatIds)
                            {
                                ElementMaterialInfo elemMaterial = new ElementMaterialInfo(einfo);
                                MaterialInfo matinfo = materialsBase[matid.IntegerValue];
                                if (matinfo.CalcType == MaterialCalcType.None)
                                {
                                    continue;
                                }
                                matinfo.IsPaintMaterial = mat.Key;
                                elemMaterial.ApplyMaterial(matinfo);
                                elemMaterials.Add(elemMaterial);
                            }
                        }
                    }
                }
            }


            int elementsCount = elemMaterials.Count;
            string folder = System.IO.Path.GetDirectoryName(dialog.FileName);
            for (int i = 0; i < elemMaterials.Count; i++)
            {
                ElementMaterialInfo emi = elemMaterials[i];
                emi.counter = i;
                emi.totalElements = elementsCount;

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
                }
            }

            TaskDialog.Show("Info", "Выгружено элементов: " + elementsCount.ToString());
            return Result.Succeeded;
        }
    }
}

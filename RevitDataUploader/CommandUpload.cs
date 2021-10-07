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

            List<LinkInstanceInfo> linkInstances = new List<LinkInstanceInfo>();
            if (linkInsts.Count > 0)
            {
                foreach (RevitLinkInstance rli in linkInsts)
                {
                    LinkInstanceInfo lii = new LinkInstanceInfo(rli);
                    linkInstances.Add(lii);
                }

                List<string> docTitles = linkInstances
                    .Select(i => i.DocTitle)
                    .Distinct()
                    .ToList();

                FormSelectLinks formLinks = new FormSelectLinks(docTitles);
                if (formLinks.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return Result.Cancelled;

                foreach (string docName in formLinks.selectedDocs)
                {
                    List<LinkInstanceInfo> curLinks = linkInstances
                        .Where(i => i.DocTitle == docName)
                        .ToList();
                    Document linkDoc = curLinks[0].RevitLinkInst.GetLinkDocument();
                    docs.Add(linkDoc);
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

                List<ElementInfo> elementsBase = new List<ElementInfo>();

                IEnumerable<Element> constructions = doc.GetConstructions(main3dView);
                foreach (Element constr in constructions)
                {
                    elementsBase.Add(new ElementInfo(constr));
                }

                IEnumerable<Element> rebars = doc.GetRebars(main3dView);
                foreach (Element rebar in rebars)
                {
                    elementsBase.AddRange(rebar.GetRebarInfos());
                }

                Dictionary<int, Dictionary<string, string>> elemIdsAndCustomParams = CustomParameterData.GetCustomParamsData(doc);
                foreach (ElementInfo ei in elementsBase)
                {
                    if (!elemIdsAndCustomParams.ContainsKey(ei.RevitElementId))
                        continue;

                    foreach (KeyValuePair<string, string> customParam in elemIdsAndCustomParams[ei.RevitElementId])
                    {
                        if (!ei.CustomParameters.ContainsKey(customParam.Key))
                            ei.CustomParameters.Add(customParam.Key, customParam.Value);
                    }
                }



                //клонирую элементы по экземплярам связей
                List<ElementInfo> elemInfosClonedByFloorsAndBlocks = new List<ElementInfo>();
                string docTitle = doc.GetTitleWithoutExtension();
                foreach (ElementInfo ei in elementsBase)
                {
                    int elemId = ei.RevitElement.Id.IntegerValue;

                    List<LinkInstanceInfo> curLinks = linkInstances
                        .Where(i => i.DocTitle == docTitle)
                        .ToList();

                    if (curLinks.Count == 0)
                    {
                        elemInfosClonedByFloorsAndBlocks.Add(ei);
                        continue;
                    }

                    foreach (LinkInstanceInfo lii in curLinks)
                    {
                        if (string.IsNullOrEmpty(lii.LinkName))
                            elemInfosClonedByFloorsAndBlocks.Add(ei);
                        else
                            elemInfosClonedByFloorsAndBlocks.Add(lii.CloneElemInfoByLink(ei));
                    }
                }

                //назначаю материалы для элементов
                foreach (ElementInfo einfo in elemInfosClonedByFloorsAndBlocks)
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

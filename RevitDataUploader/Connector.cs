using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitDataUploader
{
    public static class Connector
    {
        /// <summary>
        /// Считать данные об элементам модели раздела КР
        /// </summary>
        /// <param name="revitDocument">Документ Revit, из которого нужно выгрузить данные.</param>
        /// <returns>Список элементов модели, подготовленный для сериализации. Null если действие было отменено.</returns>
        public static List<ElementMaterialInfo> GetStructureData(Document mainDoc)
        {
            string mainDocTitle = mainDoc.GetTitleWithoutExtension();
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
                    return null;

                foreach (string docName in formLinks.selectedDocs)
                {
                    List<LinkInstanceInfo> curLinks = linkInstances
                        .Where(i => i.DocTitle == docName)
                        .ToList();
                    Document linkDoc = curLinks[0].RevitLinkInst.GetLinkDocument();
                    docs.Add(linkDoc);
                }
            }

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

                int elementsCount = elemMaterials.Count;
                for (int i = 1; i <= elementsCount; i++)
                {
                    ElementMaterialInfo emi = elemMaterials[i];
                    emi.counter = i;
                    emi.totalElements = elementsCount;
                    emi.fileName = mainDocTitle;
                }
            }
            return elemMaterials;
        }
    }
}

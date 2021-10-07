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
using Autodesk.Revit.DB.Structure;
using System.IO;
#endregion

namespace RevitDataUploader
{
    public class ElementMaterialInfo
    {
        public string name { get; set; }
        public string date { get; set; }
        public string fileName { get; set; }
        public string parentId { get; set; }
        public string uniqueId { get; set; }
        
        public int totalElements { get; set; }
        public int counter { get; set; }

        public Dictionary<string, string> parameters2 { get; set; }
        public Dictionary<string, string> parameters { get; set; }

        
        public List<MaterialInfo> materials { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public ElementInfo elemInfo;

        public ElementMaterialInfo()
        {
            //пустой конструктор для сериализатора
        }

        public ElementMaterialInfo(ElementInfo einfo)
        {
            elemInfo = einfo;
            materials = new List<MaterialInfo>();

            date = DateTime.Now.ToString();
            fileName = einfo.RevitElement.Document.Title;
            uniqueId = einfo.RevitUniqueElementId;
            if (einfo.HostElementId != null)
                parentId = einfo.HostElementId.IntegerValue.ToString();
            else
                parentId = "";

            parameters = einfo.InternalParameters;
            parameters2 = new Dictionary<string, string>();
            

            parameters2.Add("revitElementId", einfo.RevitElementId.ToString());
            parameters2.Add("revitElementName", einfo.RevitElementName);
            parameters2.Add("revitTypeName", einfo.RevitTypeName);
            parameters2.Add("revitElementNormative", einfo.RevitElementNormative);

            parameters2.Add("lengthMeters", einfo.Length.ToString());
            parameters2.Add("diameterMm", einfo.Diameter.ToString());
            parameters2.Add("count", einfo.Count.ToString());
            parameters2.Add("mark", einfo.Mark);
            parameters2.Add("constructionName", einfo.ConstructionName);
            parameters2.Add("placementOrGroup", einfo.PlacementOrGroup);
            parameters2.Add("categoryName", einfo.ElementCategory);
            parameters2.Add("ostCategoryName", einfo.ElementCategoryInternal);

            foreach (var kvp in einfo.CustomParameters)
            {
                if(!parameters2.ContainsKey(kvp.Key))
                    parameters2.Add(kvp.Key, kvp.Value);
            }


            name = einfo.ConstructionName;
            
            if(einfo.Group == ElementGroup.Rebar)
            {
                name += "_Арматура ";
            }
            else
            {
                name += "_" + einfo.RevitElementName + "_" + einfo.RevitTypeName;
            }
        }

        public void ApplyMaterial(MaterialInfo matinfo)
        {
            ElementId matid = matinfo.Material.Id;
            Element e = elemInfo.RevitElement;

            name += ": " + matinfo.Name;
            

            double qt = 0;
            switch (matinfo.CalcType)
            {
                case MaterialCalcType.None:
                    break;
                case MaterialCalcType.Items:
                    qt = 1;
                    break;
                case MaterialCalcType.Length:
                    qt = (double)elemInfo.Length;
                    break;
                case MaterialCalcType.Area:
                    double area0 = e.GetMaterialArea(matid, false);
                    matinfo.Area = area0;
                    qt = UnitUtils.ConvertFromInternalUnits(area0, DisplayUnitType.DUT_SQUARE_METERS);
                    break;
                case MaterialCalcType.Volume:
                    double volume0 = e.GetMaterialVolume(matid);
                    matinfo.Volume = volume0;
                    qt = UnitUtils.ConvertFromInternalUnits(volume0, DisplayUnitType.DUT_CUBIC_METERS);
                    break;
                case MaterialCalcType.Weight:
                    throw new Exception("Функция определения Массы материала в разработке! ElementId " + e.Id.IntegerValue.ToString());
                default:
                    return;
            }
            
            qt = Math.Round(qt, 3);
            parameters2.Add("quantity", qt.ToString("F3"));
            parameters2.Add("units", matinfo.Units);

            materials.Add(matinfo);
        }

        public void ApplyRebar(Dictionary<int, MaterialInfo> materialsBase)
        {
            MaterialInfo matInfo = new MaterialInfo();
            Element e = elemInfo.RevitElement;

            Parameter classParam = e.SuperGetParameter(Configuration.RebarClass);
            if (classParam == null || !classParam.HasValue)
                throw new Exception("Не задан Арм.КлассЧисло для " + e.Id.IntegerValue.ToString());

            long armClass = (long)classParam.AsDouble();
            if (armClass > 0)
            {
                double diam = (double)elemInfo.Diameter;
                name += " d" + diam.ToString("F0");
                name += " А" + armClass.ToString();
            }
            else if (armClass < 0)
            {
                name += "_" + e.GetParameterValAsString(Configuration.ElementName);
                string profileName = e.GetParameterValAsString("Мрк.НаименованиеСоставноеТекст1");
                if (profileName.Length > 1)
                {
                    name += "_" + profileName;
                }
                else
                {
                    name += e.GetParameterValAsString("Рзм.Толщина", " t");
                    name += e.GetParameterValAsString("Рзм.Ширина", " b");
                    name += e.GetParameterValAsString("Рзм.ДиаметрИзделия", " d");
                }
            }

            string assemblyMark = e.GetParameterValAsString(Configuration.AssemblyMark);
            if (assemblyMark.Contains("-"))
            {
                name += " в составе " + assemblyMark;
            }

            Parameter rebarUseTypeParam = e.SuperGetParameter(Configuration.RebarUseType);
            if (rebarUseTypeParam == null || !rebarUseTypeParam.HasValue)
            {
                throw new Exception("В арматуре не задан параметр " + Configuration.RebarUseType);
            }
            int rebarUseType = rebarUseTypeParam.AsInteger();
            if (rebarUseType == 4 || rebarUseType == 5)
            {
                name += " (закладные)";
            }

            double weight = RebarUtils.GetRebarWeight(elemInfo, armClass);
            parameters2.Add("quantity", weight.ToString("F3"));
            parameters2.Add("units", "кг");
            materials.Add(matInfo);
        }

        public void ApplyMetal()
        {
            throw new Exception("Выгрузка металлоконструкций в разработке!");
        }
    }
}

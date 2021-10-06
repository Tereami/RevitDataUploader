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
#endregion

namespace RevitDataUploader
{
    public enum MaterialCalcType { None, Items, Length, Area, Volume, Weight };

    [Serializable]
    public class MaterialInfo
    {
        [Newtonsoft.Json.JsonIgnore]
        public Material Material { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public MaterialCalcType CalcType { get; set; }
        [Newtonsoft.Json.JsonIgnore]
        public string Units { get; set; }

        public string Name { get; set; }
        public double Area { get; set; }
        public double Volume { get; set; }
        public bool IsPaintMaterial { get; set; }
        public string Uniqueid { get; set; }
        public int Id { get; set; }
        public string Normative { get; set; }
        
        
        public Dictionary<string, string> Parameters { get; set; }

        public MaterialInfo()
        {
            //пустой конструктор для сериализатора
        }

        public static Dictionary<int, MaterialInfo> GetAllMaterials(Document doc)
        {
            Dictionary<int, MaterialInfo> materials = new Dictionary<int, MaterialInfo>();

            FilteredElementCollector col = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            foreach (Element mat in col)
            {
                int id = mat.Id.IntegerValue;
                MaterialInfo curMatInfo = new MaterialInfo();
                curMatInfo.Material = mat as Material;
                curMatInfo.Uniqueid = mat.UniqueId;
                curMatInfo.Id = id;

                Parameter materialNameParam = mat.LookupParameter(Configuration.MaterialName);
                if (materialNameParam != null && materialNameParam.HasValue)
                    curMatInfo.Name = materialNameParam.AsString();
                else
                    curMatInfo.Name = mat.Name;

                curMatInfo.Normative = mat.GetParameterValAsString(Configuration.MaterialNormative);

                Parameter calcTypeParam = mat.LookupParameter(Configuration.MaterialCalcType);
                if (calcTypeParam != null && calcTypeParam.HasValue)
                {
                    int calcTypeInt = calcTypeParam.AsInteger();

                    switch (calcTypeInt)
                    {
                        case 0:
                            curMatInfo.CalcType = MaterialCalcType.Items;
                            curMatInfo.Units = "шт";
                            break;
                        case 1:
                            curMatInfo.CalcType = MaterialCalcType.Length;
                            curMatInfo.Units = "м";
                            break;
                        case 2:
                            curMatInfo.CalcType = MaterialCalcType.Area;
                            curMatInfo.Units = "м²";
                            break;
                        case 3:
                            curMatInfo.CalcType = MaterialCalcType.Volume;
                            curMatInfo.Units = "м³";
                            break;
                        case 4:
                            curMatInfo.CalcType = MaterialCalcType.Weight;
                            curMatInfo.Units = "кг";
                            break;
                        default:
                            curMatInfo.CalcType = MaterialCalcType.None;
                            curMatInfo.Units = "";
                            break;
                    }
                }

                curMatInfo.Parameters = new Dictionary<string, string>();
                foreach (Parameter p in mat.Parameters)
                {
                    string paramnameInternal = p.GetParameterName();
                    string paramNameUser = p.Definition.Name;
                    string value = mat.GetParameterValAsString(paramNameUser);
                    if(!curMatInfo.Parameters.ContainsKey(paramnameInternal))
                        curMatInfo.Parameters.Add(paramnameInternal, value);
                }

                materials.Add(id, curMatInfo);
            }
            return materials;
        }
    }
}

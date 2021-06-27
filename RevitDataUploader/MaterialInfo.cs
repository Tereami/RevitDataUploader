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
        [System.Xml.Serialization.XmlIgnore]
        public Material Material { get; set; }
        public string Name { get; set; }
        public MaterialCalcType CalcType { get; set; }
        public string Normative { get; set; }
        public string Units { get; set; }

        public MaterialInfo()
        {
            //пустой конструктор для сериализатора
        }

        public static Dictionary<int, MaterialInfo> GetAllMaterials(Document doc)
        {
            Dictionary<int, MaterialInfo> materials = new Dictionary<int, MaterialInfo>();

            FilteredElementCollector col = new FilteredElementCollector(doc)
                .OfClass(typeof(Material));

            foreach (Element e in col)
            {
                int id = e.Id.IntegerValue;
                MaterialInfo info = new MaterialInfo();
                info.Material = e as Material;
                Parameter materialNameParam = e.LookupParameter(Configuration.MaterialName);
                if (materialNameParam != null && materialNameParam.HasValue)
                    info.Name = materialNameParam.AsString();
                else
                    info.Name = e.Name;

                info.Normative = e.GetParameterValAsString(Configuration.MaterialNormative);

                Parameter calcTypeParam = e.LookupParameter(Configuration.MaterialCalcType);
                if (calcTypeParam == null || !calcTypeParam.HasValue)
                    continue;

                int calcTypeInt = calcTypeParam.AsInteger();

                if (calcTypeInt < 0)
                {
                    continue;
                }
                switch (calcTypeInt)
                {
                    case 0:
                        info.CalcType = MaterialCalcType.Items;
                        info.Units = "шт";
                        break;
                    case 1:
                        info.CalcType = MaterialCalcType.Length;
                        info.Units = "м";
                        break;
                    case 2:
                        info.CalcType = MaterialCalcType.Area;
                        info.Units = "м²";
                        break;
                    case 3:
                        info.CalcType = MaterialCalcType.Volume;
                        info.Units = "м³";
                        break;
                    case 4:
                        info.CalcType = MaterialCalcType.Weight;
                        info.Units = "кг";
                        break;
                    default:
                        continue;
                }

                materials.Add(id, info);
            }
            return materials;
        }
    }
}

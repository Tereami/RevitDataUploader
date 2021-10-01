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
#endregion

namespace RevitDataUploader
{
    public class ItemInfo
    {
        public string name { get; set; }

        public string date { get; set; }

        public string fileName { get; set; }

        public string parentId { get; set; }

        public int totalElements { get; set; }
        public int counter { get; set; }

        public string uniqueId { get; set; }


        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string,string> InternalParameters { get; set; }

        public List<ItemMaterialInfo> materials { get; set; }


        public ItemInfo()
        {
            //пустой конструктор для сериализатора
        }

        public ItemInfo(ElementMaterialInfo emi)
        {
            name = emi.FullName;
            date = DateTime.Now.ToString();
            fileName = emi.ElemInfo.RevitElement.Document.Title;
            materials = new List<ItemMaterialInfo>();
            uniqueId = emi.ElemInfo.RevitUniqueElementId;

            if (emi.ElemInfo.HostElementId != null)
                parentId = emi.ElemInfo.HostElementId.IntegerValue.ToString();
            else
                parentId = "";

            Dictionary<string, string> tempParams = new Dictionary<string, string>();

            tempParams.Add("quantity", emi.Quantity.ToString("F3"));
            tempParams.Add("units", emi.MatInfo.Units);

            tempParams.Add("revitElementId", emi.ElemInfo.RevitElementId);
            tempParams.Add("revitElementName", emi.ElemInfo.RevitElementName);
            tempParams.Add("revitTypeName", emi.ElemInfo.RevitTypeName);
            tempParams.Add("revitElementNormative", emi.ElemInfo.RevitElementNormative);
            
            tempParams.Add("lengthMeters", emi.ElemInfo.Length.ToString());
            tempParams.Add("diameterMm", emi.ElemInfo.Diameter.ToString());
            tempParams.Add("count", emi.ElemInfo.Count.ToString());
            tempParams.Add("mark", emi.ElemInfo.Mark);
            tempParams.Add("constructionName", emi.ElemInfo.ConstructionName);
            tempParams.Add("placementOrGroup", emi.ElemInfo.PlacementOrGroup);
            tempParams.Add("categoryName", emi.ElemInfo.ElementCategory);
            tempParams.Add("ostCategoryName", emi.ElemInfo.ElementCategoryInternal);

            foreach (ParameterInfo pi in emi.ElemInfo.CustomParameters)
            {
                tempParams.Add(pi.Name, pi.Value);
            }

            if (emi.MatInfo != null)
            {
                ItemMaterialInfo imi = new ItemMaterialInfo(emi);
                materials.Add(imi);
            }
            Parameters = tempParams;

            InternalParameters = emi.ElemInfo.InternalParameters;
        }
    }
}

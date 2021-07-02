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
        public string Name { get; set; }

        public string FileName { get; set; }

        public double Quantity { get; set; }

        public string Units { get; set; }

        public List<ParameterInfo> Parameters { get; set; }


        public ItemInfo()
        {
            //пустой конструктор для сериализатора
        }

        public ItemInfo(ElementMaterialInfo emi)
        {
            Name = emi.FullName;
            FileName = emi.ElemInfo.RevitElement.Document.Title;
            Quantity = emi.Quantity;

            List<ParameterInfo> ps = new List<ParameterInfo>();
            ps.Add(new ParameterInfo("RevitElementId", emi.ElemInfo.RevitElementId));
            ps.Add(new ParameterInfo("RevitElementName", emi.ElemInfo.RevitElementName));
            ps.Add(new ParameterInfo("RevitTypeName", emi.ElemInfo.RevitTypeName));
            ps.Add(new ParameterInfo("RevitElementNormative", emi.ElemInfo.RevitElementNormative));
            ps.Add(new ParameterInfo("RevitUniqueElementId", emi.ElemInfo.RevitUniqueElementId));
            ps.Add(new ParameterInfo("Length", emi.ElemInfo.Length.ToString(), "м"));
            ps.Add(new ParameterInfo("Diameter", emi.ElemInfo.Diameter.ToString(), "мм"));
            ps.Add(new ParameterInfo("Count", emi.ElemInfo.Count.ToString()));
            ps.Add(new ParameterInfo("Mark", emi.ElemInfo.Mark));
            ps.Add(new ParameterInfo("ConstructionName", emi.ElemInfo.ConstructionName));
            ps.Add(new ParameterInfo("PlacementOrGroup", emi.ElemInfo.PlacementOrGroup));
            ps.Add(new ParameterInfo("Category", emi.ElemInfo.Category));

            ps.AddRange(emi.ElemInfo.CustomParameters);

            if (emi.MatInfo != null)
            {
                if (emi.MatInfo.Material != null)
                    ps.Add(new ParameterInfo("MaterialId", emi.MatInfo.Material.Id.IntegerValue.ToString()));

                if (emi.MatInfo.Name != null)
                    ps.Add(new ParameterInfo("MaterialName", emi.MatInfo.Name));

                if (emi.MatInfo.Normative != null)
                    ps.Add(new ParameterInfo("MaterialNormative", emi.MatInfo.Normative));

                ps.Add(new ParameterInfo("MaterialCalcType", Enum.GetName(typeof(MaterialCalcType), emi.MatInfo.CalcType)));

                if(emi.MatInfo.Units != null)
                    Units = emi.MatInfo.Units;
            }
            Parameters = ps;
        }
    }
}

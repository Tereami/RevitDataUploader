﻿#region License
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
    public enum ElementGroup { Concrete, Metal, Isolation, Rebar }

    [Serializable]
    public class ElementInfo
    {
        public bool IsValid { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public Element RevitElement { get; set; }
        public ElementGroup ElementType { get; set; }

        public string RevitElementName { get; set; }
        public string RevitTypeName { get; set; }
        public string RevitElementId { get; set; }
        public string RevitElementNormative { get; set; }
        public string RevitUniqueElementId { get; set; }
        public string Units { get; set; }
        public double? Length { get; set; }
        public double? Diameter { get; set; }
        public double Count { get; set; }
        public string Mark { get; set; }
        public string ConstructionName { get; set; }
        public string PlacementOrGroup { get; set; }
        public string Category { get; set; }

        List<CustomParameter> CustomParameters;

        public ElementInfo()
        {
            //пустой конструктор для сериализатора
        }

        public ElementInfo(Element elem)
        {
            RevitElement = elem;
            RevitElementId = elem.Id.IntegerValue.ToString();
            RevitElementNormative = elem.GetParameterValAsString(Configuration.ElementNorvative);

            RevitUniqueElementId = elem.UniqueId;
            
            Length = elem.GetLength();
            Diameter = elem.GetDiameter();
            Count = elem.GetCount();

            BuiltInCategory bic = (BuiltInCategory)elem.Category.Id.IntegerValue;


            if (bic == BuiltInCategory.OST_Rebar)
                ElementType = ElementGroup.Rebar;
            else if (elem is RoofBase)
                ElementType = ElementGroup.Isolation;
            else if (elem is FamilyInstance)
            {
                FamilyInstance fi = elem as FamilyInstance;
                Parameter metalGroupConstr = fi.SuperGetParameter(Configuration.MetalGroupConstr);
                if (metalGroupConstr != null && metalGroupConstr.HasValue)
                    ElementType = ElementGroup.Metal;

                if (fi.Symbol.FamilyName.StartsWith("222"))
                    ElementType = ElementGroup.Isolation;
            }
            else
                ElementType = ElementGroup.Concrete;

            Mark = elem.GetMark();
            ConstructionName = ParameterUtils.GetConstructionByMark(Mark);
            RevitElementName = elem.GetParameterValAsString(Configuration.ElementName);

            ElementId typeId = elem.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                ElementType elemType = elem.Document.GetElement(elem.GetTypeId()) as ElementType;
                RevitTypeName = elemType.Name;
            }

            IsValid = true;
        }
    }
}
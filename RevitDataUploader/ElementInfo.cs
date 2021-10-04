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
    public enum ElementGroup { Concrete, Metal, Isolation, Rebar }

    public class ElementInfo
    {
        public bool IsValid { get; set; }

        public Element RevitElement { get; set; }
        public ElementGroup Group { get; set; }

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
        public string ElementCategory { get; set; }
        public string ElementCategoryInternal { get; set; }
        public string FamilyName { get; set; }

        public ElementId HostElementId { get; set; }

        public List<ParameterInfo> CustomParameters = new List<ParameterInfo>();
        public Dictionary<string, string> InternalParameters = new Dictionary<string, string>();

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

            ElementCategory = elem.Category.Name;
            BuiltInCategory bic = (BuiltInCategory)elem.Category.Id.IntegerValue;
            ElementCategoryInternal = Enum.GetName(typeof(BuiltInCategory), bic);



            if (bic == BuiltInCategory.OST_Rebar)
                Group = ElementGroup.Rebar;
            else if (elem is RoofBase)
                Group = ElementGroup.Isolation;
            else if (elem is FamilyInstance)
            {
                FamilyInstance fi = elem as FamilyInstance;
                Parameter metalGroupConstr = fi.SuperGetParameter(Configuration.MetalGroupConstr);
                if (metalGroupConstr != null && metalGroupConstr.HasValue)
                    Group = ElementGroup.Metal;

                if (fi.Symbol.FamilyName.StartsWith("222"))
                    Group = ElementGroup.Isolation;
            }
            else
                Group = ElementGroup.Concrete;

            Mark = elem.GetMark();
            ConstructionName = ParameterUtils.GetConstructionByMark(Mark);
            RevitElementName = elem.GetParameterValAsString(Configuration.ElementName);

            ElementId typeId = elem.GetTypeId();
            if (typeId != ElementId.InvalidElementId)
            {
                ElementType elemType = elem.Document.GetElement(elem.GetTypeId()) as ElementType;
                RevitTypeName = elemType.Name;
            }

            foreach(Parameter p in elem.Parameters)
            {
                string paramnameInternal = p.GetParameterName();
                string paramNameUser = p.Definition.Name;
                string value = elem.GetParameterValAsString(paramNameUser);
                InternalParameters.Add(paramnameInternal, value);
            }

            ElementId elemTypeId = elem.GetTypeId();
            if(elemTypeId != null && elemTypeId != ElementId.InvalidElementId)
            {
                ElementType elemType = elem.Document.GetElement(elemTypeId) as ElementType;
                if(elemType != null)
                {
                    foreach (Parameter p in elemType.Parameters)
                    {
                        string paramnameInternal = p.GetParameterName();
                        string paramNameUser = p.Definition.Name;
                        string value = elem.GetParameterValAsString(paramNameUser);
                        if(!InternalParameters.ContainsKey(paramnameInternal))
                            InternalParameters.Add(paramnameInternal, value);
                    }
                }
            }

            HostElementId = elem.SuperGetHostId();

            IsValid = true;
        }
    }
}

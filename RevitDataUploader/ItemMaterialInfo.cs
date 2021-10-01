using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitDataUploader
{
    public class ItemMaterialInfo
    {
        public string Name { get; set; }
        public double Area { get; set; }
        public double Volume { get; set; }
        public bool IsPaintMaterial { get; set; }
        public string Uniqueid { get; set; }
        public int Id { get; set; }
        public string fileName { get; set; }
        public Dictionary<string,string> Parameters { get; set; }


        public ItemMaterialInfo(ElementMaterialInfo emi)
        {
            Parameters = new Dictionary<string, string>();

            Name = emi.MatInfo.Name;
            Area = emi.MatInfo.Area;
            Volume = emi.MatInfo.Volume;
            IsPaintMaterial = emi.MatInfo.IsPaint;
            Uniqueid = emi.MatInfo.Material.UniqueId;
            Id = emi.MatInfo.Material.Id.IntegerValue;
            fileName = emi.ElemInfo.RevitElement.Document.Title;

            foreach(Parameter p in emi.MatInfo.Material.ParametersMap)
            {
                string paramnameInternal = p.GetParameterName();
                string paramNameUser = p.Definition.Name;
                string value = emi.MatInfo.Material.GetParameterValAsString(paramNameUser);
                Parameters.Add(paramnameInternal, value);
            }
        }
    }
}


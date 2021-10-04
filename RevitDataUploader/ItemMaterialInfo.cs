/*using System;
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
            fileName = emi.ElemInfo.RevitElement.Document.Title;
            Area = emi.MatInfo.Area;
            Volume = emi.MatInfo.Volume;
            IsPaintMaterial = emi.MatInfo.IsPaint;
            Uniqueid = "";
            Id = -1;
            if (emi.MatInfo.Material != null)
            {
                
            }
        }
    }
}
*/


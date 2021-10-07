using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace RevitDataUploader
{
    public class LinkInstanceInfo 
    {
        public string DocTitle { get; set; }
        public int LinkId { get; set; }
        public string LinkName { get; set; }
        public string FloorName { get; set; }
        public RevitLinkInstance RevitLinkInst { get; set; }

        public LinkInstanceInfo(RevitLinkInstance rli)
        {
            RevitLinkInst = rli;
            DocTitle = rli.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM).AsValueString();
            if (DocTitle.EndsWith(".rvt"))
                DocTitle = DocTitle.Replace(".rvt", "");

            LinkName = rli.get_Parameter(BuiltInParameter.RVT_LINK_INSTANCE_NAME).AsString();

            Parameter linkFloorParamName = rli.LookupParameter(Configuration.FloorParamName); 
            if(linkFloorParamName != null && linkFloorParamName.HasValue)
            {
                FloorName = linkFloorParamName.AsString();
            }            
        }

        public ElementInfo CloneElemInfoByLink(ElementInfo sourceElemInfo)
        {
            ElementInfo cloneEi = (ElementInfo)sourceElemInfo.Clone();

            cloneEi.CustomParameters = sourceElemInfo.CustomParameters.ToDictionary(i => i.Key, i => i.Value);
            cloneEi.CustomParameters.Add(Configuration.BlockParamName, "Блок №" + LinkName);

            if (string.IsNullOrEmpty(FloorName))
            {
                cloneEi.Mark += "-" + LinkName;
            }
            else
            {
                cloneEi.CustomParameters.Add(Configuration.FloorParamName, FloorName);
                cloneEi.Mark += FloorName + "-" + LinkName;
            }

            return cloneEi;
        }
    }
}

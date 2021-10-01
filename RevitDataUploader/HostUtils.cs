using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace RevitDataUploader
{
    public static class HostUtils
    {
        public static ElementId SuperGetHostId(this Element selElem)
        {
            ElementId hostId = null;

            if (selElem is AreaReinforcement)
            {
                AreaReinforcement el = selElem as AreaReinforcement;
                hostId = el.GetHostId();
            }
            else if (selElem is PathReinforcement)
            {
                PathReinforcement el = selElem as PathReinforcement;
                hostId = el.GetHostId();
            }
            else if (selElem is Rebar)
            {
                Rebar el = selElem as Rebar;
                hostId = el.GetHostId();
            }
            else if (selElem is RebarInSystem)
            {
                RebarInSystem el = selElem as RebarInSystem;
                hostId = el.SystemId;
            }
            else if (selElem is FamilyInstance)
            {
                FamilyInstance el = selElem as FamilyInstance;
                Element host = el.Host;
                if (host != null)
                {
                    hostId = host.Id;
                }
                else
                {
                    Element parentFamily = el.SuperComponent;
                    if (parentFamily != null)
                    {
                        hostId = parentFamily.Id;
                    }
                }
            }

            return hostId;
        }
    }
}

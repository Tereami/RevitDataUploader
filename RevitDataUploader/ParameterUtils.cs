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
using Autodesk.Revit.DB.Structure;
#endregion

namespace RevitDataUploader
{
    public static class ParameterUtils
    {
        public static Parameter SuperGetParameter(this Element Elem, string ParamName)
        {

            Parameter param = Elem.LookupParameter(ParamName);
            if (param == null)
            {
                Element eltype = Elem.Document.GetElement(Elem.GetTypeId());
                param = eltype.LookupParameter(ParamName);
            }
            return param;
        }

        public static string GetParameterValAsString(this Element e, string paramName, string prefix = "")
        {
            Parameter param = e.SuperGetParameter(paramName);
            if (param == null) return string.Empty;

            string val = string.Empty;

            switch (param.StorageType)
            {
                case StorageType.None:
                    return string.Empty;
                case StorageType.Integer:
                    val = param.AsInteger().ToString();
                    break;
                case StorageType.Double:
                    val = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), param.DisplayUnitType).ToString("0.###");
                    break;
                case StorageType.String:
                    val = param.AsString();
                    break;
                case StorageType.ElementId:
                    val = param.AsElementId().IntegerValue.ToString();
                    break;
            }
            if (string.IsNullOrEmpty(val))
                return string.Empty;
            else
                return prefix + val;
        }

        public static double GetLength(this Element elem)
        {
            double length = 0;

            if (elem is Wall)
            {
                length = elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            }
            else if (elem is Rebar || elem is RebarInSystem)
            {
                length = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_LENGTH).AsDouble();
            }
            else
            {
                Parameter lengthParam = elem.get_Parameter(BuiltInParameter.STRUCTURAL_FRAME_CUT_LENGTH);
                if (lengthParam == null || !lengthParam.HasValue)
                    lengthParam = elem.SuperGetParameter(Configuration.BeamTrueLength);
                if (lengthParam == null || !lengthParam.HasValue)
                    lengthParam = elem.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM);
                if (lengthParam == null || !lengthParam.HasValue)
                    lengthParam = elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);
                if (lengthParam == null || !lengthParam.HasValue)
                    lengthParam = elem.SuperGetParameter(Configuration.Length);
                if (lengthParam != null && lengthParam.HasValue)
                {
                    length = lengthParam.AsDouble();
                }
            }

            if (length < 0.001) return 0;

            double lengthMeters0 = UnitUtils.ConvertFromInternalUnits(length, DisplayUnitType.DUT_METERS);
            double lengthMm = lengthMeters0 * 1000;
            double lengthMmRound5 = 5 * Math.Round(lengthMm / 5, MidpointRounding.AwayFromZero);
            double lengthMetersRound = lengthMmRound5 / 1000;
            return lengthMetersRound;
        }

        public static double? GetDiameter(this Element elem)
        {
            Parameter diamParam = elem.SuperGetParameter(Configuration.AssemblyDiameter);
            if (diamParam == null)
                diamParam = elem.SuperGetParameter(Configuration.Diameter);

            if (diamParam == null)
                return null;
            else
            {
                double diameter = diamParam.AsDouble();
                double diamMm = UnitUtils.ConvertFromInternalUnits(diameter, DisplayUnitType.DUT_MILLIMETERS);
                diamMm = Math.Round(diamMm);
                return diamMm;
            }
        }

        public static double GetCount(this Element e)
        {
            Parameter rebarCountParam = e.get_Parameter(BuiltInParameter.REBAR_ELEM_QUANTITY_OF_BARS);
            if(rebarCountParam != null && rebarCountParam.HasValue)
            {
                int rebarCount = rebarCountParam.AsInteger();
                return (double)rebarCount;
            }

            Parameter countParam = e.SuperGetParameter(Configuration.Count);
            if (countParam != null && countParam.HasValue)
                return countParam.AsDouble();

            return 1;
        }

        public static string GetMark(this Element elem)
        {
            if (elem is Rebar || elem is RebarInSystem)
            {
                string mark = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_HOST_MARK).AsString();
                return mark;
            }
            else
            {
                Parameter myMarkParam = elem.SuperGetParameter(Configuration.Mark);
                if (myMarkParam != null && myMarkParam.HasValue)
                {
                    string mark = myMarkParam.AsString();
                    return mark;
                }
            }

            Parameter markParam = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            if (markParam != null && markParam.HasValue)
            {
                string mark = markParam.AsString();
                return mark;
            }
            return "INVALID_MARK";
        }



        public static string GetConstructionByMark(string mark)
        {
            if(mark.Contains("-"))
                mark = mark.Split('-').First();

            if (Configuration.markBase.ContainsKey(mark))
            {
                return Configuration.markBase[mark];
            }
            else
            {
                return "неизвестная конструкция";
            }
        }


        public static string GetPlacement(this Element elem)
        {
            Parameter placeParam = null;

            if (elem is Rebar || elem is RebarInSystem)
            {
                placeParam = elem.get_Parameter(BuiltInParameter.NUMBER_PARTITION_PARAM);
            }
            else
            {
                placeParam = elem.SuperGetParameter(Configuration.Placement);
            }

            if (placeParam != null && placeParam.HasValue)
            {
                string place = placeParam.AsString();
                return place;
            }
            else
            {
                return null;
            }

        }
    }
}

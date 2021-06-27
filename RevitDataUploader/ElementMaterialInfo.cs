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
using System.IO;
#endregion

namespace RevitDataUploader
{
    public class ElementMaterialInfo
    {
        public string FullName { get; set; }
        public double Quantity { get; set; }
        public ElementInfo ElemInfo { get; set; }
        public MaterialInfo MatInfo { get; set; }


        public ElementMaterialInfo()
        {
            //пустой конструктор для сериализатора
        }

        public ElementMaterialInfo(ElementInfo einfo)
        {
            ElemInfo = einfo;
            FullName = einfo.ConstructionName + "_" + einfo.RevitElementName + "_" + einfo.RevitTypeName;
        }

        public void ApplyMaterial(MaterialInfo matinfo)
        {
            MatInfo = matinfo;
            ElementId matid = matinfo.Material.Id;
            Element elem = ElemInfo.RevitElement;

            FullName += ": " + matinfo.Name;

            switch (matinfo.CalcType)
            {
                case MaterialCalcType.None:
                    break;
                case MaterialCalcType.Items:
                    Quantity = 1;
                    break;
                case MaterialCalcType.Length:
                    Quantity = (double)ElemInfo.Length;
                    break;
                case MaterialCalcType.Area:
                    double area0 = elem.GetMaterialArea(matid, false);
                    Quantity = UnitUtils.ConvertFromInternalUnits(area0, DisplayUnitType.DUT_SQUARE_METERS);
                    break;
                case MaterialCalcType.Volume:
                    double volume0 = elem.GetMaterialVolume(matid);
                    Quantity = UnitUtils.ConvertFromInternalUnits(volume0, DisplayUnitType.DUT_CUBIC_METERS);
                    break;
                case MaterialCalcType.Weight:
                    throw new Exception("Функция определения Массы материала в разработке! ElementId " + elem.Id.IntegerValue.ToString());
                default:
                    return;
            }
            Quantity = Math.Round(Quantity, 6);
        }

        public void ApplyRebar()
        {
            Element e = ElemInfo.RevitElement;
            MaterialInfo rebarMaterial = new MaterialInfo();
            
            //rebarMaterial.Name;

            Parameter classParam = e.SuperGetParameter(Configuration.RebarClass);
            if (classParam == null || !classParam.HasValue)
                throw new Exception("Не задан Арм.КлассЧисло для " + e.Id.IntegerValue.ToString());

            long armClass = (long)classParam.AsDouble();
            if (armClass > 0)
            {
                FullName += "_Арматура";
                double diam = (double)ElemInfo.Diameter;
                double diameterMm = UnitUtils.ConvertFromInternalUnits(diam, DisplayUnitType.DUT_MILLIMETERS);
                FullName += " d" + diameterMm.ToString("F0");
                FullName += " А" + armClass.ToString();
            }
            else if (armClass < 0)
            {
                FullName += "_" + e.SuperGetParameter(Configuration.ElementName);
                string profileName = e.GetParameterValAsString("Мрк.НаименованиеСоставноеТекст1");
                if (profileName.Length > 1)
                {
                    FullName += "_" + profileName;
                }
                else
                {
                    FullName += e.GetParameterValAsString("Рзм.Толщина", " t");
                    FullName += e.GetParameterValAsString("Рзм.Ширина", " b");
                    FullName += e.GetParameterValAsString("Рзм.ДиаметрИзделия", " d");
                }
            }

            string assemblyMark = e.GetParameterValAsString(Configuration.AssemblyMark);
            if (assemblyMark.Contains("-"))
            {
                Parameter rebarUseTypeParam = e.SuperGetParameter(Configuration.RebarUseType);
                if (rebarUseTypeParam == null || !rebarUseTypeParam.HasValue)
                {
                    throw new Exception("В арматуре не задан параметр Орг.ИзделиеТипПодсчета");
                }
                int rebarUseType = rebarUseTypeParam.AsInteger();
                if(rebarUseType == 4 || rebarUseType == 5)
                {
                    FullName += " в составе закладной детали " + assemblyMark;
                }
                else
                {
                    FullName += " в составе арматурного изделия " + assemblyMark;
                }
            }

            double weight = RebarUtils.GetRebarWeight(this.ElemInfo, armClass);

            this.Quantity = weight;
            rebarMaterial.Units = "кг";
        }

        public void ApplyMetal()
        {
            throw new Exception("Выгрузка металлоконструкций в разработке!");
        }
    }
}
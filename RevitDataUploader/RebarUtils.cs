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
    public static class RebarUtils
    {
        public static double GetRebarWeight(ElementInfo einfo, long RebarClass)
        {
            Element elem = einfo.RevitElement;
            Parameter rebarIsFamilyParam = elem.SuperGetParameter(Configuration.RebarIsFamily);
            if (rebarIsFamilyParam == null) return 0;

            bool rebarIsFamily = rebarIsFamilyParam.AsInteger() == 1;

            double count = einfo.Count;
            double diameterMm = (double)einfo.Diameter;
            double lengthMm = (double)einfo.Length * 1000;

            double WeightPerMeter = 0;


            if (RebarClass > 0)
            {
                WeightPerMeter = GetWeightPerMeter(diameterMm);
            }
            else
            {
                Parameter WeightPerMeterParam = elem.SuperGetParameter(Configuration.WeightPerMeter);
                if (WeightPerMeterParam == null)
                    throw new Exception("Нет параметра " + Configuration.WeightPerMeter + " в элементе " + elem.Id.IntegerValue.ToString());

                WeightPerMeter = WeightPerMeterParam.AsDouble();
            }


            double WeightOnePcs = 0;
            double countPM = 0;
            double overlapCoeff = 1;

            Parameter asSummLengthParam = elem.SuperGetParameter(Configuration.CountAsSumLength);
            if (asSummLengthParam == null)
                throw new Exception("Нет параметра " + Configuration.CountAsSumLength + " в элементе " + elem.Id.IntegerValue.ToString());

            bool calcAsSummLength = (asSummLengthParam.AsInteger() == 1);
            if (calcAsSummLength)
            {
                WeightOnePcs = WeightPerMeter;
                overlapCoeff = 1;
                if (!rebarIsFamily && RebarClass > 0)
                {
                    double concreteClass = 0;
                    Parameter concreteClassParam = elem.SuperGetParameter(Configuration.RebarConcreteClass);
                    if (concreteClassParam == null)
                        concreteClass = 15;
                    else 
                        concreteClass = concreteClassParam.AsDouble();

                    double Rs = GetRs(RebarClass);
                    double Rbt = GetRbt(concreteClass);

                    double mm32 = 1;
                    if (diameterMm > 32) mm32 = 0.9;

                    //формула для основного шаблона weandrevit
                    //overlapCoeff = 1 + 0.001 * Math.Ceiling((1.2 * Rs * diameterMm) / (2.5 * mm32 * Rbt * 4 * 11.75));

                    //адаптированная формула под smlt
                    overlapCoeff = 11700 / (11700 - (1.2 * Rs * diameterMm) / (2.5 * mm32 * Rbt * 4));
                }

                countPM = 0.1 * Math.Round(lengthMm * count * overlapCoeff / 100, MidpointRounding.AwayFromZero);
            }
            else
            {
                double m1 = WeightPerMeter * lengthMm;
                double m2 = Math.Ceiling(m1);
                double m3 = 0.001 * m2;
                double m4 = Math.Round(m3, 3, MidpointRounding.AwayFromZero);
                WeightOnePcs = m4;
                countPM = count;
                overlapCoeff = 1;
            }

            double wf1 = countPM * WeightOnePcs;
            double wf2 = 10000 * wf1;
            double wf3 = Math.Round(wf2);
            double wf4 = wf3 * 0.01;
            double wf5 = Math.Round(wf4, MidpointRounding.AwayFromZero);
            double wf6 = 0.01 * wf5;
            double wf7 = Math.Round(wf6, 3);
            double weightFinal = wf7;

            return weightFinal;
        }


        public static Dictionary<double, double> RebarDiameterWeightBase = new Dictionary<double, double> {
                { 3, 0.055 },
                { 4, 0.098 },
                { 5, 0.153 },
                { 6, 0.222 },
                { 8, 0.395 },
                { 10, 0.617 },
                { 12, 0.888 },
                { 14, 1.208 },
                { 16, 1.578 },
                { 18, 1.998 },
                { 20, 2.465 },
                { 22, 2.984 },
                { 25, 3.85 },
                { 28, 4.83 },
                { 32, 6.31 },
                { 36, 7.99 },
                {40, 9.865 }
            };

        public static double GetWeightPerMeter(double diameterMm)
        {
            if (RebarDiameterWeightBase.ContainsKey(diameterMm))
                return RebarDiameterWeightBase[diameterMm];

            double diamSq = Math.Pow(diameterMm , 2);
            double WeightCalc = 7.85 * (Math.PI * diamSq / 4000);
            return WeightCalc;
        }

        public static Dictionary<double, double> RbtBase = new Dictionary<double, double>
        {
            { 10, 0.56},
            { 15, 0.75},
            { 20, 0.9 },
            { 25, 1.05},
            { 30, 1.15},
            { 35, 1.3 },
            { 40, 1.4 }
        };

        public static double GetRbt(double concreteClass)
        {
            if (RbtBase.ContainsKey(concreteClass))
                return RbtBase[concreteClass];

            throw new Exception("Отсутствует класс бетона в базе: " + concreteClass.ToString("F0"));
        }

        public static double GetRs(double RebarClass)
        {
            if (RebarClass == 240) return 215;
            if (RebarClass == 400) return 355;
            if (RebarClass == 500) return 435;

            throw new Exception("Нет класса арматуры в базе: " + RebarClass.ToString("F0"));
        }
    }
}

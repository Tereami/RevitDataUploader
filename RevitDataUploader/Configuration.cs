using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitDataUploader
{
    public static class Configuration
    {
        public const string ElementNorvative = "О_Обозначение";
        public const string ElementName = "О_Наименование";
        public const string MaterialCalcType = "О_Материал тип подсчета";
        public const string MaterialName = "Мтрл.Название";
        public const string MaterialNormative = "О_Материал обозначение";
        
        public const string Length = "Рзм.Длина";
        public const string Diameter = "Рзм.Диаметр";
        public const string AssemblyDiameter = "Рзм.ДиаметрИзделия";

        public const string BeamTrueLength = "Рзм.ДлинаБалкиИстинная";
        public const string CountAsSumLength = "Рзм.ПогМетрыВкл";

        public const string Mark = "Мрк.МаркаКонструкции";
        public const string AssemblyMark = "Мрк.МаркаИзделия";

        public const string Placement = "Орг.ЗонаРасположения";

        public const string MetalGroupConstr = "КМ.ГруппаКонструкций";

        public const string RebarClass = "Арм.КлассЧисло";
        public const string RebarUseType = "Орг.ИзделиеТипПодсчета";
        public const string Count = "О_Количество";
        public const string RebarIsFamily = "Арм.ВыполненаСемейством";
        public const string RebarConcreteClass = "Арм.КлассБетона";

        public const string WeightPerMeter = "О_МассаПогМетра";


        public static Dictionary<string, string> markBase = new Dictionary<string, string> { 
            { "СТм", "Стена"},
            { "Пм", "Пилон монолитный"},
            { "Бм", "Балка монолитная"},
            { "ППм", "Плита перекрытия"},
            { "Км", "Колонна монолитная"},
            { "ПР", "Приямок"},
            { "РСм", "Ростверк монолитный"},
            { "Св", "Свая"},
            { "РПм", "Рампа монолитная"},
            { "ФПм", "Фундаментная плита"},
            { "ФЛм", "Фундаментная лента"},
            { "ФСм", "Фундаментный стакан"},
            { "ПЛм", "Площадка лестничная"},
            { "МЛм", "Марш лестничный"},
            { "МЛс", "Марш лестничный сборный"},
            { "ППЛм", "Плита перекрытия приямка"},
            { "КПм", "Капитель монолитная"},
            { "ФОм", "Фундамент под оборудование"},
            { "ПРм", "Парапет монолитный"}
        };

    }
}

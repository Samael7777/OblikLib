using System;

namespace Oblik
{
    //Структуры данных
    public struct DayGraphRow                                               //Структура строки суточного графика
    {
        public DateTime time;       //время записи
        public float act_en_p;      //активная энергия "+" за период сохранения 
        public float act_en_n;      //активная энергия "-" за период сохранения
        public float rea_en_p;      //реактивная энергия "+" за период сохранения
        public float rea_en_n;      //реактивная энергия "-" за период сохранения
        public ushort[] channel;    //Количество импульсов по каналам
    }
    public struct CalcUnitsStruct                                           //Структура параметров вычислений
    {
        public float
            ener_fct,
            powr_fct,
            curr_fct,
            volt_fct,
            curr_1w,
            curr_2w,
            volt_1w,
            volt_2w,
            pwr_lim_A,
            pwr_lim_B,
            pwr_lim_C,
            pwr_lim_D;
        public sbyte
            ener_unit,
            powr_unit,
            curr_unit,
            volt_unit;
        public byte save_const;
    }
    public struct FirmwareVer                                               //Структура версии ПО счетчика
    {
        public int Version;     
        public int Build;
    } 
    public struct CurrentValues
    {
        public float
            curr1,
            curr2,
            curr3,
            volt1,
            volt2,
            volt3,
            act_pw,
            rea_pw;
        public UInt16 freq;
    }
}

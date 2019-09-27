//Структуры данных счетчика

using System;

namespace Oblik
{
    //Структуры данных
    /// <summary>
    /// Структура строки суточного графика
    /// </summary>
    public struct DayGraphRow
    {
        /// <summary>
        /// Время записи
        /// </summary>
        public DateTime time;
        /// <summary>
        /// Активная энергия "+" за период сохранения
        /// </summary>
        public float act_en_p;
        /// <summary>
        /// Активная энергия "-" за период сохранения
        /// </summary>
        public float act_en_n;
        /// <summary>
        /// Реактивная энергия "+" за период сохранения
        /// </summary>
        public float rea_en_p;
        /// <summary>
        /// Реактивная энергия "-" за период сохранения
        /// </summary>
        public float rea_en_n;
        /// <summary>
        /// Количество импульсов по каналам
        /// </summary>
        public ushort[] channel;
    }

    /// <summary>
    /// Структура параметров вычислений
    /// </summary>
    public struct CalcUnitsStruct
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

    /// <summary>
    /// Структура версии ПО счетчика
    /// </summary>
    public struct FirmwareVer
    {
        public int Version;
        public int Build;
    }

    /// <summary>
    /// Структура текущих показателей
    /// </summary>
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

    /// <summary>
    /// Уровень доступа
    /// 0 - пользователь;
    /// 1 - администратор;
    /// 2 - энергонадзор; 
    /// 3 - служебный пользователь.
    /// </summary>
    public enum AccessLevel
    {
        User = 0,
        Admin = 1,
        Energo = 2,
        System = 3
    }

    /// <summary>
    /// Структрура параметров соединения со счетчиком
    /// </summary>
    public struct OblikConnection
    {
        /// <summary>
        /// Номер COM-порта
        /// </summary>
        public int? Port;
        /// <summary>
        /// Скорость соединения
        /// </summary>
        public int? Baudrate;
        /// <summary>
        /// Таймаут соединения
        /// </summary>
        public int? Timeout;
        /// <summary>
        /// Количество попыток соединения
        /// </summary>
        public int? Repeats;
        /// <summary>
        /// Адрес счетчика в сети RS-485
        /// </summary>
        public int? Address;
        /// <summary>
        /// Пароль от счетчика
        /// </summary>
        public string Password;
        /// <summary>
        /// Уровень доступа
        /// </summary>
        public AccessLevel? AccessLevel;
    }
}

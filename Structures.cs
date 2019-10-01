//Структуры данных счетчика

using System;

namespace OblikControl
{
    //Структуры данных
    /// <summary>
    /// Структура строки суточного графика
    /// </summary>
    public struct DayGraphRow
    {
        /// <summary>
        /// Реактивная энергия "+" за период сохранения
        /// </summary>
        public float Rea_en_p { get; set; }
        /// <summary>
        /// Время записи
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// Активная энергия "+" за период сохранения
        /// </summary>
        public float Act_en_p { get; set; }
        /// <summary>
        /// Активная энергия "-" за период сохранения
        /// </summary>
        public float Act_en_n { get; set; }
        /// <summary>
        /// Реактивная энергия "-" за период сохранения
        /// </summary>
        public float Rea_en_n { get; set; }
        /// <summary>
        /// Количество импульсов по каналам
        /// </summary>
        public int[] Channel { get; set; }
    }

    /// <summary>
    /// Структура параметров вычислений
    /// </summary>
    public struct CalcUnitsStruct
    {
        public float Ener_fct { get; set; }
        public float Powr_fct { get; set; }
        public float Curr_fct { get; set; }
        public float Volt_fct { get; set; }
        public float Curr_1w { get; set; }
        public float Curr_2w { get; set; }
        public float Volt_1w { get; set; }
        public float Volt_2w { get; set; }
        public float Pwr_lim_A { get; set; }
        public float Pwr_lim_B { get; set; }
        public float Pwr_lim_C { get; set; }
        public float Pwr_lim_D { get; set; }
        public byte Save_const { get; set; }
        public sbyte Ener_unit { get; set; }
        public sbyte Powr_unit { get; set; }
        public sbyte Curr_unit { get; set; }
        public sbyte Volt_unit { get; set; }
    }

    /// <summary>
    /// Структура версии ПО счетчика
    /// </summary>
    public struct FirmwareVer
    {
        public int Version { get; set; }
        public int Build { get; set; }
    }

    /// <summary>
    /// Структура текущих показателей
    /// </summary>
    public struct CurrentValues
    {
        public float Curr1 { get; set; }
        public float Curr2 { get; set; }
        public float Curr3 { get; set; }
        public float Volt1 { get; set; }
        public float Volt2 { get; set; }
        public float Volt3 { get; set; }
        public float Act_pw { get; set; }
        public float Rea_pw { get; set; }
        public ushort Freq { get; set; }
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
        /// <summary>
        /// Пользователь
        /// </summary>
        User = 0,
        /// <summary>
        /// Администратор
        /// </summary>
        Admin = 1,
        /// <summary>
        /// Энергонадзор
        /// </summary>
        Energo = 2,
        /// <summary>
        /// Системный пользователь
        /// </summary>
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
        public int? Port { get; set; }
        /// <summary>
        /// Скорость соединения
        /// </summary>
        public int? Baudrate { get; set; }
        /// <summary>
        /// Таймаут соединения
        /// </summary>
        public int? Timeout { get; set; }
        /// <summary>
        /// Количество попыток соединения
        /// </summary>
        public int? Repeats { get; set; }
        /// <summary>
        /// Адрес счетчика в сети RS-485
        /// </summary>
        public int? Address { get; set; }
        /// <summary>
        /// Пароль от счетчика
        /// </summary>
        public string Password { get; set; }
        /// <summary>
        /// Уровень доступа
        /// </summary>
        public AccessLevel? AccessLevel { get; set; }
    }

    /// <summary>
    /// Структура записи карты сегментов
    /// </summary>
    public struct SegmentsMapRec
    {
        /// <summary>
        /// Номер сегмента
        /// </summary>
        public byte Num { get; set; }
        /// <summary>
        /// Права доступа
        /// </summary>
        public byte Right { get; set; }
        /// <summary>
        /// Размер сегмента в байтах
        /// </summary>
        public ushort Size { get; set; }
        /// <summary>
        /// Режим доступа: 0 - чтение, 1 - запись
        /// </summary>
        public int Accsess { get; set; }
    }
    /// <summary>
    /// Структура сетевой конфигурации счетчика
    /// </summary>
    public struct NetworkConfig
    {
        /// <summary>
        /// Сетевой адрес по протоколу RS-48
        /// </summary>
        public byte Addr { get; set; }
        /// <summary>
        /// Скорость соединения, делитель от 115200
        /// </summary>
        public ushort Divisor { get; set; }
    }
}

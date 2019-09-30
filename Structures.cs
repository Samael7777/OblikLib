//Структуры данных счетчика

using System;
using System.Collections.Generic;

namespace OblikControl
{
    //Структуры данных
    /// <summary>
    /// Структура строки суточного графика
    /// </summary>
    public struct DayGraphRow
    {
        private DateTime time;
        private float act_en_p;
        private float act_en_n;
        private float rea_en_p;
        private float rea_en_n;
        private int[] channel;
        /// <summary>
        /// Реактивная энергия "+" за период сохранения
        /// </summary>
        public float Rea_en_p { get => rea_en_p; set => rea_en_p = value; }
        /// <summary>
        /// Время записи
        /// </summary>
        public DateTime Time { get => time; set => time = value; }
        /// <summary>
        /// Активная энергия "+" за период сохранения
        /// </summary>
        public float Act_en_p { get => act_en_p; set => act_en_p = value; }
        /// <summary>
        /// Активная энергия "-" за период сохранения
        /// </summary>
        public float Act_en_n { get => act_en_n; set => act_en_n = value; }
        /// <summary>
        /// Реактивная энергия "-" за период сохранения
        /// </summary>
        public float Rea_en_n { get => rea_en_n; set => rea_en_n = value; }
        /// <summary>
        /// Количество импульсов по каналам
        /// </summary>
        public int[] Channel { get => channel; set => channel = value; }
    }

    /// <summary>
    /// Структура параметров вычислений
    /// </summary>
    public struct CalcUnitsStruct
    {
        private float
            pwr_lim_D;
        private sbyte
            volt_unit;
        private byte save_const;
        private float ener_fct;
        private float powr_fct;
        private float curr_fct;
        private float volt_fct;
        private float curr_1w;
        private float curr_2w;
        private float volt_1w;
        private float volt_2w;
        private float pwr_lim_A;
        private float pwr_lim_B;
        private float pwr_lim_C;
        private sbyte ener_unit;
        private sbyte powr_unit;
        private sbyte curr_unit;

        public float Ener_fct { get => ener_fct; set => ener_fct = value; }
        public float Powr_fct { get => powr_fct; set => powr_fct = value; }
        public float Curr_fct { get => curr_fct; set => curr_fct = value; }
        public float Volt_fct { get => volt_fct; set => volt_fct = value; }
        public float Curr_1w { get => curr_1w; set => curr_1w = value; }
        public float Curr_2w { get => curr_2w; set => curr_2w = value; }
        public float Volt_1w { get => volt_1w; set => volt_1w = value; }
        public float Volt_2w { get => volt_2w; set => volt_2w = value; }
        public float Pwr_lim_A { get => pwr_lim_A; set => pwr_lim_A = value; }
        public float Pwr_lim_B { get => pwr_lim_B; set => pwr_lim_B = value; }
        public float Pwr_lim_C { get => pwr_lim_C; set => pwr_lim_C = value; }
        public float Pwr_lim_D { get => pwr_lim_D; set => pwr_lim_D = value; }
        public byte Save_const { get => save_const; set => save_const = value; }
        public sbyte Ener_unit { get => ener_unit; set => ener_unit = value; }
        public sbyte Powr_unit { get => powr_unit; set => powr_unit = value; }
        public sbyte Curr_unit { get => curr_unit; set => curr_unit = value; }
        public sbyte Volt_unit { get => volt_unit; set => volt_unit = value; }
    }

    /// <summary>
    /// Структура версии ПО счетчика
    /// </summary>
    public struct FirmwareVer
    {
        private int version;
        private int build;

        public int Version { get => version; set => version = value; }
        public int Build { get => build; set => build = value; }
    }

    /// <summary>
    /// Структура текущих показателей
    /// </summary>
    public struct CurrentValues
    {
        private float rea_pw;
        private UInt16 freq;
        private float curr1;
        private float curr2;
        private float curr3;
        private float volt1;
        private float volt2;
        private float volt3;
        private float act_pw;

        public float Curr1 { get => curr1; set => curr1 = value; }
        public float Curr2 { get => curr2; set => curr2 = value; }
        public float Curr3 { get => curr3; set => curr3 = value; }
        public float Volt1 { get => volt1; set => volt1 = value; }
        public float Volt2 { get => volt2; set => volt2 = value; }
        public float Volt3 { get => volt3; set => volt3 = value; }
        public float Act_pw { get => act_pw; set => act_pw = value; }
        public float Rea_pw { get => rea_pw; set => rea_pw = value; }
        public ushort Freq { get => freq; set => freq = value; }
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
        
        private int? port;
        private int? baudrate;
        private int? timeout;
        private int? repeats;
        private int? address;
        private string password;
        private AccessLevel? accessLevel;
       
        /// <summary>
        /// Номер COM-порта
        /// </summary>
        public int? Port { get => port; set => port = value; }
        /// <summary>
        /// Скорость соединения
        /// </summary>
        public int? Baudrate { get => baudrate; set => baudrate = value; }
        /// <summary>
        /// Таймаут соединения
        /// </summary>
        public int? Timeout { get => timeout; set => timeout = value; }
        /// <summary>
        /// Количество попыток соединения
        /// </summary>
        public int? Repeats { get => repeats; set => repeats = value; }
        /// <summary>
        /// Адрес счетчика в сети RS-485
        /// </summary>
        public int? Address { get => address; set => address = value; }
        /// <summary>
        /// Пароль от счетчика
        /// </summary>
        public string Password { get => password; set => password = value; }
        /// <summary>
        /// Уровень доступа
        /// </summary>
        public AccessLevel? AccessLevel { get => accessLevel; set => accessLevel = value; }
    }

    /// <summary>
    /// Структура записи карты сегментов
    /// </summary>
    public struct SegmentsMapRec
    {
        private byte num;
        private byte right;
        private ushort size;
        private int accsess;
        
        /// <summary>
        /// Номер сегмента
        /// </summary>
        public byte Num { get => num; set => num = value; }
        /// <summary>
        /// Права доступа
        /// </summary>
        public byte Right { get => right; set => right = value; }
        /// <summary>
        /// Размер сегмента в байтах
        /// </summary>
        public ushort Size { get => size; set => size = value; }
        /// <summary>
        /// Режим доступа: 0 - чтение, 1 - запись
        /// </summary>
        public int Accsess { get => accsess; set => accsess = value; }
    }
    /// <summary>
    /// Структура сетевой конфигурации счетчика
    /// </summary>
    public struct NetworkConfig
    {
        /// <summary>
        /// Сетевой адрес по протоколу RS-485
        /// </summary>
        public byte addr;
        /// <summary>
        /// Скорость соединения, делитель от 115200
        /// </summary>
        public ushort divisor;
    }
}

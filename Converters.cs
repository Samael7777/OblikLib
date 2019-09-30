//Преобразователи данных

using System;
using System.IO;
using System.Collections.Generic;

namespace OblikControl
{
    public partial class Oblik
    {
        //Группа преобразователей массива байт в различные типы данных и наоборот. 
        //Принимается, что старший байт имеет младший адрес (big-endian)

        /// <summary>
        /// Преобразование массива байт в UInt32
        /// </summary>
        /// <param name="array"></param>
        /// <returns>Число UInt32</returns>
        private static UInt32 ToUint32(byte[] array)
        {
            Array.Reverse(array);
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream = null;
                    return reader.ReadUInt32();
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Преобразование UInt32 в массив байт
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] UInt32ToByte(UInt32 data)
        {
            byte[] res = new byte[sizeof(UInt32)];
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(res);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    writer.Write(data);
                    Array.Reverse(res);
                    return res;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Преобразование массива байт в float
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static float ToFloat(byte[] array)
        {
            Array.Reverse(array);
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream = null;
                    return reader.ReadSingle();
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Преобразование float в массив байт
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] FloatToByte(float data)
        {
            byte[] res = new byte[sizeof(float)];
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(res);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    stream = null;
                    writer.Write(data);
                    Array.Reverse(res);
                    return res;
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Преобразование массива байт в word (оно же uint16)
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static UInt16 ToUint16(byte[] array)
        {
            Array.Reverse(array);
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    stream = null;
                    return reader.ReadUInt16();
                }
            }
            finally
            {
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Преобразование word(UInt16) в массив байт
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] UInt16ToByte(UInt16 data)
        {
            byte[] res = new byte[2];
            res[0] = (byte)((data & 0xFF00) >> 8);
            res[1] = (byte)(data & 0x00FF);
            return res;
        }

        /// <summary>
        /// Преобразование массива байт в дату и время
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static DateTime ToUTCTime(byte[] array)
        {
            UInt32 _ctime;  //Время по стандарту t_time
            DateTime BaseTime;
            BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);       //Базовая точка времени 01.01.1970 00:00 GMT
            _ctime = ToUint32(array);                                             //Время в формате C (time_t) 
            return BaseTime.AddSeconds(_ctime);
        }

        /// <summary>
        /// Преобразование массива байт в uminiflo 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static float ToUminiflo(byte[] array)
        {
            UInt16 _data = ToUint16(array);
            UInt16 man, exp;
            float res;
            man = (UInt16)(_data & 0x7FF);                                      //Мантисса - биты 0-10
            exp = (UInt16)((_data & 0xF800) >> 11);                             //Порядок - биты 11-15
            res = (float)System.Math.Pow(2, (exp - 15)) * (1 + man / 2048);     //Pow - возведение в степень
            return res;
        }

        /// <summary>
        /// Преобразование массива байт в sminiflo
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static float ToSminiflo(byte[] array)
        {
            UInt16 _data = ToUint16(array);
            UInt16 sig = (UInt16)(_data & (UInt16)1);                                  //Знак - бит 0
            UInt16 man = (UInt16)((_data & 0x7FE) >> 1);                               //Мантисса - биты 1-10
            UInt16 exp = (UInt16)((_data & 0xF800) >> 11);                             //Порядок - биты 11-15
            return (float)(Math.Pow(2, exp - 15) * (1 + (man / 2048)) * Math.Pow(-1, sig));     //Pow - возведение в степень
        }

        /// <summary>
        /// Преобразование массива байт в строку суточного графика
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static DayGraphRow ToDayGraphRow(byte[] array)
        {
            DayGraphRow res = new DayGraphRow();
            int index = 0;
            //time (4 байта)
            res.Time = ToUTCTime(ArrayPart(array, index, 4)).ToLocalTime();
            index += 4;
            //act_en_p (2 байта)
            res.Act_en_p = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;
            //act_en_n (2 байта)
            res.Act_en_n = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;
            //rea_en_p (2 байта)
            res.Rea_en_p = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;
            //rea_en_n (2 байта)
            res.Rea_en_n = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;
            res.Channel = new int[8];
            for (int i = 0; i <= 7; i++)
            {
                res.Channel[i] = (int)ToUint16(ArrayPart(array, index, sizeof(ushort)));
                index += 2;
            }
            return res;
        }

        /// <summary>
        /// Преобразование DateTime в массив байт согласно t_time
        /// </summary>
        /// <param name="Date"></param>
        /// <returns></returns>
        private static byte[] ToTime(DateTime Date)
        {
            DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);      //Базовая точка времени 01.01.1970 00:00 GMT
            UInt32 Time = (UInt32)(Date - BaseTime).TotalSeconds;
            return UInt32ToByte(Time);
        }

        /// <summary>
        /// Преобразование массива байт в структуру параметров вычислений
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static CalcUnitsStruct ToCalcUnits(byte[] array)
        {
            CalcUnitsStruct res = new CalcUnitsStruct();


            int index = 0;
            res.Ener_fct = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Powr_fct = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Curr_fct = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Volt_fct = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            //Reserved1
            index += sizeof(float);

            res.Ener_unit = (sbyte)array[index];
            index++;

            res.Powr_unit = (sbyte)array[index];
            index++;

            res.Curr_unit = (sbyte)array[index];
            index++;

            res.Volt_unit = (sbyte)array[index];
            index++;

            res.Curr_1w = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Curr_2w = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Volt_1w = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Volt_2w = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Save_const = array[index];
            index++;

            res.Pwr_lim_A = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Pwr_lim_B = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Pwr_lim_C = ToFloat(ArrayPart(array, index, sizeof(float)));
            index += sizeof(float);

            res.Pwr_lim_D = ToFloat(ArrayPart(array, index, sizeof(float)));

            return res;
        }

        /// <summary>
        /// Преобразование структуры параметров вычислений в массив байт
        /// </summary>
        /// <param name="CalcUnits"></param>
        /// <returns></returns>
        private static byte[] CalcUnitsToByte(CalcUnitsStruct CalcUnits)
        {
            byte[] res = new byte[57];
            int index = 0;

            FloatToByte(CalcUnits.Ener_fct).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Powr_fct).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Curr_fct).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Volt_fct).CopyTo(res, index);
            index += sizeof(float);

            //reserved1
            for (int i = 0; i <= 3; i++)
            {
                res[index] = 0;
                index++;
            }

            res[index] = (byte)(CalcUnits.Ener_unit);
            index++;

            res[index] = (byte)(CalcUnits.Powr_unit);
            index++;

            res[index] = (byte)(CalcUnits.Curr_unit);
            index++;

            res[index] = (byte)(CalcUnits.Volt_unit);
            index++;

            FloatToByte(CalcUnits.Curr_1w).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Curr_2w).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Volt_1w).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Volt_2w).CopyTo(res, index);
            index += sizeof(float);

            res[index] = CalcUnits.Save_const;
            index++;

            FloatToByte(CalcUnits.Pwr_lim_A).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Pwr_lim_B).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Pwr_lim_C).CopyTo(res, index);
            index += sizeof(float);

            FloatToByte(CalcUnits.Pwr_lim_D).CopyTo(res, index);

            return res;
        }

        /// <summary>
        /// Преобразование массива байт в структуру текущих значений
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private static CurrentValues ToCurrentValues(byte[] array)
        {
            CurrentValues res = new CurrentValues();
            
            int index = 0;
            res.Curr1 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Curr2 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Curr3 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Volt1 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Volt2 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Volt3 = ToUminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Act_pw = ToSminiflo(ArrayPart(array, index, 2));
            index += 2;

            res.Rea_pw = ToSminiflo(ArrayPart(array, index, 2));
            index += 2;

            //Reserved1
            index += 2;

            res.Freq = ToUint16(ArrayPart(array, index, 2));

            return res;
        }

        /// <summary>
        /// Преобразовывает массив байт в запись карты сегментов
        /// </summary>
        /// <param name="array">Исходный массив</param>
        /// <returns></returns>
        private static SegmentsMapRec ToSegmentsMapRec(byte[] array)
        {
            SegmentsMapRec res = new SegmentsMapRec
            {
                Num = array[0],
                Right = (byte)(array[1] & 15),
                Accsess = (array[1] & 128) >> 7,
                Size = ToUint16(ArrayPart(array, 2, 2))
            };
            return res;
        }

        /// <summary>
        /// Отдает массив заданной длины, начинающийся с заданного индекса исходного массива
        /// </summary>
        /// <param name="array">Источник</param>
        /// <param name="StartIndex">Начальный индекс</param>
        /// <param name="Lenght">Длина</param>
        /// <returns>Массив байт</returns>
        private static byte[] ArrayPart(byte[] array, int StartIndex, int Lenght)
        {
            byte[] res = new byte[Lenght];
            Array.Copy(array, StartIndex, res, 0, Lenght);
            return res;
        }

        /// <summary>
        /// Конвертирует структуру сетевых настроек в массив байт
        /// </summary>
        /// <param name="nc">Структура сетевых настроек</param>
        /// <returns>Массив байт</returns>
        private static byte[] NetworkConfigToByte (NetworkConfig nc)
        {
            byte[] res = new byte[3];
            res[0] = nc.addr;
            UInt16ToByte(nc.divisor).CopyTo(res, 1);
            return res;
        }
    }
}

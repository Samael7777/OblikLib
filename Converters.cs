//Преобразователи данных

using System;
using System.IO;

namespace Oblik
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
        private UInt32 ToUint32(byte[] array)
        {
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
        private byte[] UInt32ToByte(UInt32 data)
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
        private float ToFloat(byte[] array)
        {
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
        private byte[] FloatToByte(float data)
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
        private UInt16 ToUint16(byte[] array)
        {
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
        private byte[] UInt16ToByte(UInt16 data)
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
        private DateTime ToUTCTime(byte[] array)
        {
            UInt32 _ctime;  //Время по стандарту t_time
            DateTime BaseTime, Time;
            BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);       //Базовая точка времени 01.01.1970 00:00 GMT
            _ctime = ToUint32(array);                                             //Время в формате C (time_t) 
            Time = BaseTime.AddSeconds(_ctime);
            return Time;
        }

        /// <summary>
        /// Преобразование массива байт в uminiflo 
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private float ToUminiflo(byte[] array)
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
        private float ToSminiflo(byte[] array)
        {
            UInt16 _data = ToUint16(array);
            UInt16 man, exp, sig;
            float res;
            sig = (UInt16)(_data & (UInt16)1);                                  //Знак - бит 0
            man = (UInt16)((_data & 0x7FE) >> 1);                               //Мантисса - биты 1-10
            exp = (UInt16)((_data & 0xF800) >> 11);                             //Порядок - биты 11-15
            res = (float)(System.Math.Pow(2, (exp - 15)) * (1 + man / 2048) * System.Math.Pow(-1, sig));     //Pow - возведение в степень
            return res;
        }

        /// <summary>
        /// Преобразование массива байт в строку суточного графика
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private DayGraphRow ToDayGraphRow(byte[] array)
        {
            DayGraphRow res = new DayGraphRow();
            byte[] _tmp = new byte[0];
            int index = 0;
            //time (4 байта)
            Array.Resize(ref _tmp, 4);
            Array.Copy(array, index, _tmp, 0, 4);
            res.time = ToUTCTime(_tmp).ToLocalTime();
            index += 4;
            //act_en_p (2 байта)
            Array.Resize(ref _tmp, 2);
            Array.Copy(array, index, _tmp, 0, 2);
            res.act_en_p = ToUminiflo(_tmp);
            index += 2;
            //act_en_n (2 байта)
            Array.Copy(array, index, _tmp, 0, 2);
            res.act_en_n = ToUminiflo(_tmp);
            index += 2;
            //rea_en_p (2 байта)
            Array.Copy(array, index, _tmp, 0, 2);
            res.rea_en_p = ToUminiflo(_tmp);
            index += 2;
            //rea_en_n (2 байта)
            Array.Copy(array, index, _tmp, 0, 2);
            res.rea_en_n = ToUminiflo(_tmp);
            index += 2;
            res.channel = new ushort[8];
            for (int i = 0; i < 8; i++)
            {
                Array.Copy(array, index, _tmp, 0, 2);
                res.channel[i] = ToUint16(_tmp);
                index += 2;
            }
            return res;
        }

        /// <summary>
        /// Преобразование DateTime в массив байт согласно t_time
        /// </summary>
        /// <param name="Date"></param>
        /// <returns></returns>
        private byte[] ToTime(DateTime Date)
        {
            DateTime BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);      //Базовая точка времени 01.01.1970 00:00 GMT
            UInt32 Time;                                                                  //Время по стандарту t_time
            byte[] Res = new byte[4];
            Time = (UInt32)(Date - BaseTime).TotalSeconds;
            Res[0] = (byte)((Time >> 24) & 0xff);
            Res[1] = (byte)((Time >> 16) & 0xff);
            Res[2] = (byte)((Time >> 8) & 0xff);
            Res[3] = (byte)(Time & 0xff);
            return Res;
        }

        /// <summary>
        /// Преобразование массива байт в структуру параметров вычислений
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private CalcUnitsStruct ToCalcUnits(byte[] array)
        {
            CalcUnitsStruct res = new CalcUnitsStruct();
            byte[] tmp = new byte[4];

            int index = 0;
            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.ener_fct = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.powr_fct = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.curr_fct = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.volt_fct = ToFloat(tmp);
            index += sizeof(float);

            //Reserved1
            index += sizeof(float);

            Array.Resize(ref tmp, 1);
            res.ener_unit = (sbyte)array[index];
            index++;

            res.powr_unit = (sbyte)array[index];
            index++;

            res.curr_unit = (sbyte)array[index];
            index++;

            res.volt_unit = (sbyte)array[index];
            index++;

            Array.Resize(ref tmp, 4);
            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.curr_1w = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.curr_2w = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.volt_1w = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.volt_2w = ToFloat(tmp);
            index += sizeof(float);

            Array.Resize(ref tmp, 1);
            res.save_const = array[index];
            index++;

            Array.Resize(ref tmp, 4);
            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.pwr_lim_A = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.pwr_lim_B = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.pwr_lim_C = ToFloat(tmp);
            index += sizeof(float);

            Array.Copy(array, index, tmp, 0, sizeof(float));
            res.pwr_lim_D = ToFloat(tmp);

            return res;
        }

        /// <summary>
        /// Преобразование структуры параметров вычислений в массив байт
        /// </summary>
        /// <param name="CalcUnits"></param>
        /// <returns></returns>
        private byte[] CalcUnitsToByte(CalcUnitsStruct CalcUnits)
        {
            byte[] res = new byte[57];
            int index = 0;
            byte[] tmp;

            tmp = FloatToByte(CalcUnits.ener_fct);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.ener_fct);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.powr_fct);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.curr_fct);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.volt_fct);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            //reserved1
            tmp[0] = 0;
            tmp[1] = 0;
            tmp[2] = 0;
            tmp[3] = 0;
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            res[index] = (byte)(CalcUnits.ener_unit);
            index++;

            res[index] = (byte)(CalcUnits.powr_unit);
            index++;

            res[index] = (byte)(CalcUnits.curr_unit);
            index++;

            res[index] = (byte)(CalcUnits.volt_unit);
            index++;

            tmp = FloatToByte(CalcUnits.curr_1w);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.curr_2w);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.volt_1w);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.volt_2w);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            res[index] = CalcUnits.save_const;
            index++;

            tmp = FloatToByte(CalcUnits.pwr_lim_A);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.pwr_lim_B);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.pwr_lim_C);
            Array.Copy(tmp, 0, res, index, sizeof(float));
            index += sizeof(float);

            tmp = FloatToByte(CalcUnits.pwr_lim_D);
            Array.Copy(tmp, 0, res, index, sizeof(float));

            return res;
        }

        /// <summary>
        /// Преобразование массива байт в структуру текущих значений
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        private CurrentValues ToCurrentValues(byte[] array)
        {
            CurrentValues res = new CurrentValues();
            byte[] tmp = new byte[2];
            int index = 0;
            Array.Copy(array, index, tmp, 0, 2);
            res.curr1 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.curr2 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.curr3 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.volt1 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.volt2 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.volt3 = ToUminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.act_pw = ToSminiflo(tmp);
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.rea_pw = ToSminiflo(tmp);
            index += 2;

            //Reserved1
            index += 2;

            Array.Copy(array, index, tmp, 0, 2);
            res.freq = ToUint16(tmp);

            return res;
        }
    }
}

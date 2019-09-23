//Операции с сегментами счетчика

using System;
using System.Collections.Generic;


namespace Oblik
{
    public partial class Oblik : IOblik
    {
        /// <summary>
        /// Получить количество строк суточного графика (сегмент #44)
        /// </summary>
        /// <returns>Количество строк суточного графика или -1 в случае ошибки</returns>
        public int DayGraphRecs
        {
            get
            {
                const byte segment = 44;
                const ushort offset = 0;
                if (SegmentAccsess(segment, offset, 2, null, Access.Read))
                {
                    //Порядок байт в счетчике - обратный по отношению к пк, переворачиваем
                    return (int)(L2Data[0] + (int)(L2Data[1] << 8));
                }
                else return -1;
            }
        }

        /// <summary>
        /// Очистка суточного графика (сегмент #88)
        /// </summary>
        /// <returns>Успех</returns>
        public bool CleanDayGraph()
        {
            const byte segment = 88;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_addr);
            cmd[1] = (byte)_addr;
            SegmentAccsess(segment, offset, (byte)cmd.Length, cmd, Access.Write);
            return _isError;
        }

        /// <summary>
        /// Установка текущего времени в счетчике (сегмент #65)
        /// </summary>
        /// <returns>Успех</returns>
        public bool SetCurrentTime()
        {
            const byte segment = 65;
            const ushort offset = 0;
            DateTime CurrentTime = System.DateTime.Now.ToUniversalTime();        //Текущее время в формате UTC
            CurrentTime.AddSeconds(2);                                           //2 секунды на вычисление, отправку и т.д.
            byte[] Buf = ToTime(CurrentTime);
            SegmentAccsess(segment, offset, (byte)Buf.Length, Buf, Access.Write);
            return _isError;
        }

        /// <summary>
        /// Получить суточный график  (сегмент #45)
        /// </summary>
        /// <param name="lines">Количество строк в запросе</param>
        /// <param name="offset">Смещение относительно начала, в строках</param>
        /// <returns>Список строк суточного графика</returns>
        public List<DayGraphRow> GetDayGraphList(uint lines, uint offset)
        {
            List<DayGraphRow> res = new List<DayGraphRow>();
            const byte segment = 45;                                //Сегмент суточного графика
            const uint LineLen = 28;                                //28 байт на 1 строку данных по протоколу счетчика
            const uint MaxReqLines = 8;                             //Максимальное количество строк в запросе
            const byte MaxReqBytes = (byte)(LineLen * MaxReqLines); //Максимальный размер запроса в байтах
            byte[] _buf;                                            //Буфер
            uint TotalLines = (uint)DayGraphRecs;              //Количество строк суточного графика фактически в счетчике

            //Если запрос выходит за диапазон или нет строк для чтения, то выход
            if ((TotalLines == 0) || ((lines + offset) > TotalLines))
            {
                ChangeStatus("Нет записей для чтения", false);
                SetProgress(100);
                return res;
            }
            uint OffsetBytes = offset * LineLen;
            uint BytesReq = (lines - offset) * LineLen;                     //Всего запрошено байт
            _buf = new byte[BytesReq];
            ushort curroffs = 0;                                            //Текущий сдвиг в байтах
            ushort maxoffs = (ushort)(OffsetBytes + (lines - 1) * LineLen); //Максимальный сдвиг для чтения последней строки
            byte bytestoread = MaxReqBytes;                                 //Байт в запросе
            uint LinesRead = 0;                                             //Счетчик считанных строк
            float Progress = 0;                                             //Прогресс выполнения операции
            float ProgressStep = (float)(100.0 / BytesReq);                 //Прогресс на 1 запрошенный байт
            while (curroffs <= maxoffs)
            {
                if (((BytesReq - curroffs) / MaxReqBytes) == 0)
                {
                    bytestoread = (byte)((BytesReq - curroffs) % MaxReqBytes);
                }

                if (SegmentAccsess(segment, curroffs, bytestoread, null, Access.Read))
                {
                    Progress += ProgressStep * bytestoread;
                    SetProgress(Progress);                                      //Вызов события прогресса
                    Array.Resize(ref _buf, (int)(curroffs + LineLen));
                    Array.Copy(L2Data, 0, _buf, curroffs, L2Data.Length);       //Результат считывания помещается в L2Data
                    curroffs += bytestoread;
                    LinesRead += bytestoread / LineLen;
                    //Получение из ответа структуры суточного графика
                    for (int i = 0; i < LinesRead; i++)
                    {
                        byte[] _tmp = new byte[LineLen];
                        Array.Copy(_buf, (i * LineLen), _tmp, 0, LineLen);
                        res.Add(ToDayGraphRow(_tmp));
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Получить версию ПО счетчика (сегмент #44)
        /// </summary>
        /// <param name="fw">Структура версии ПО</param>
        /// <returns>Успех</returns>
        public bool GetFWVersion(out FirmwareVer fw)
        {
            fw = new FirmwareVer();
            const byte segment = 44;
            const ushort offset = 0;
            const byte len = 2;
            if (SegmentAccsess(segment, offset, len, null, Access.Read))
            {
                fw.Version = (int)L2Data[0];
                fw.Build = (int)L2Data[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Получить текущие (усредненные за 2 сек.) значения электроэнергии (сегмент #36)
        /// </summary>
        /// <param name="values">Структура текущих значений</param>
        /// <returns>Успех</returns>
        public bool GetCurrentValues(out CurrentValues values)
        {
            const byte segment = 36;
            const UInt16 offset = 0;
            const byte len = 33;
            SegmentAccsess(segment, offset, len, null, 0);
            if (!_isError)
            {
                values = ToCurrentValues(L2Data);
            }
            else
            {
                values = new CurrentValues();
            }
            return _isError;
        }

        /// <summary>
        /// Получить параметры вычислений из счетчика (сегмент #56)
        /// </summary>
        private void GetCalcUnits()
        {
            byte segment = 56;                                      //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte len = 57;                                          //Размер данных сегмента
            SegmentAccsess(segment, offset, len, null, Access.Read);
            if (!_isError || (L2Data.Length != 57)) { return; }
            _CalcUnits = ToCalcUnits(L2Data);
        }

        /// <summary>
        /// //Записать параметры вычислений из свойства объекта в счетчик (сегмент #57)
        /// </summary>
        private void SetCalcUnits()
        {
            byte segment = 57;                                     //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte[] data = CalcUnitsToByte(_CalcUnits);
            SegmentAccsess(segment, offset, (byte)data.Length, data, Access.Write);
        }

    }
}

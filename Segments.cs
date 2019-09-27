//Операции с сегментами счетчика

using System;
using System.Collections.Generic;


namespace Oblik
{
    public partial class Oblik
    {
        /// <summary>
        /// Получить количество строк суточного графика (сегмент #44)
        /// </summary>
        /// <returns>Количество строк суточного графика или -1 в случае ошибки</returns>
        public int DayGraphRecs()
        {
            const byte segment = 44;
            const ushort offset = 0;
            const byte len = 2;
            if (SegmentRead(segment, offset, len, out byte[] QueryResult))
            {
                return ToUint16(QueryResult);
            }
            return -1;
        }

        /// <summary>
        /// Очистка суточного графика (сегмент #88)
        /// </summary>
        /// <returns>Успех операции</returns>
        public bool CleanDayGraph()
        {
            const byte segment = 88;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_ConParams.Address);
            cmd[1] = (byte)_ConParams.Address;
            return SegmentWrite(segment, offset, cmd);
        }

        /// <summary>
        /// Очистка протокола событий (сегмент #89)
        /// </summary>
        /// <returns>Успех операции</returns>
        public bool CleanEventsLog()
        {
            const byte segment = 89;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_ConParams.Address);
            cmd[1] = (byte)_ConParams.Address;
            return SegmentWrite(segment, offset, cmd);
        }

        /// <summary>
        /// Установка текущего времени в счетчике (сегмент #65)
        /// </summary>
        /// <returns>Успех</returns>
        public bool SetCurrentTime()
        {
            const byte segment = 65;
            const ushort offset = 0;
            DateTime CurrentTime = DateTime.Now.ToUniversalTime();          //Текущее время в формате UTC
            CurrentTime.AddSeconds(2);                                      //2 секунды на вычисление, отправку и т.д.
            byte[] Buf = ToTime(CurrentTime);
            return SegmentWrite(segment, offset, Buf);
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
            int TotalLines = DayGraphRecs();                        //Количество строк суточного графика фактически в счетчике

            //Если запрос выходит за диапазон или нет строк для чтения, то выход
            if ((TotalLines <= 0) || ((lines + offset) > TotalLines))
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
            byte[] QueryResult;                                             //Данные от 1 запроса
            while (curroffs <= maxoffs)
            {
                if (((BytesReq - curroffs) / MaxReqBytes) == 0)
                {
                    bytestoread = (byte)((BytesReq - curroffs) % MaxReqBytes);
                }

                if (SegmentRead(segment, curroffs, bytestoread, out QueryResult))
                {
                    Progress += ProgressStep * bytestoread;
                    SetProgress(Progress);                                      //Вызов события прогресса
                    Array.Resize(ref _buf, (int)(curroffs + LineLen));
                    Array.Copy(QueryResult, 0, _buf, curroffs, QueryResult.Length);       //Результат считывания помещается в L2Data
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
        /// Получить версию ПО счетчика (сегмент #2)
        /// </summary>
        /// <param name="FirmwareVer">Структура версии ПО</param>
        /// <returns>Успех</returns>
        public bool GetFWVersion(out FirmwareVer FirmwareVer)
        {
            FirmwareVer = new FirmwareVer();
            const byte segment = 2;
            const ushort offset = 0;
            const byte len = 2;
            if (SegmentRead(segment, offset, len, out byte[] QueryResult))
            {
                FirmwareVer.Version = (int)QueryResult[0];
                FirmwareVer.Build = (int)QueryResult[1];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Получить текущие (усредненные за 2 сек.) значения электроэнергии (сегмент #36)
        /// </summary>
        /// <param name="values">Структура текущих значений</param>
        /// <returns>Успех операции</returns>
        public bool GetCurrentValues(out CurrentValues Values)
        {
            Values = new CurrentValues();
            const byte segment = 36;
            const UInt16 offset = 0;
            const byte len = 33;

            if (!SegmentRead(segment, offset, len, out byte[] QueryResult))
            {
                return false;
            }
            Values = ToCurrentValues(QueryResult);
            return true;
        }

        /// <summary>
        /// Получить параметры вычислений из счетчика (сегмент #56)
        /// </summary>
        /// <returns>Успех операции</returns>
        private bool GetCalcUnits()
        {
            byte segment = 56;                                      //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte len = 57;
            if (!SegmentRead(segment, offset, len, out byte[] QueryResult)) { return false; }
            _CalcUnits = ToCalcUnits(QueryResult);
            return true;
        }

        /// <summary>
        /// //Записать параметры вычислений из свойства объекта в счетчик (сегмент #57)
        /// </summary>
        /// <returns>Успех операции</returns>
        private bool SetCalcUnits()
        {
            byte segment = 57;                                     //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte[] data = CalcUnitsToByte(_CalcUnits);
            if (!SegmentWrite(segment, offset, data)) { return false; }
            return true;
        }

    }
}

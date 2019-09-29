//Операции с сегментами счетчика

using System;
using System.Collections.Generic;
using OblikControl.Resources;


namespace OblikControl
{
    public partial class Oblik
    {
        /// <summary>
        /// Получить количество строк суточного графика (сегмент #44)
        /// </summary>
        /// <returns>Количество строк суточного графика или -1 в случае ошибки</returns>
        public int GetDayGraphRecs()
        {
            const byte segment = 44;
            const ushort offset = 0;
            const byte len = 2;
            if (SegmentRead(segment, offset, len, out byte[] QueryResult))
            {
                int res = ToUint16(QueryResult);
                return res;
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
        public List<DayGraphRow> GetDayGraphList(int lines, int offset)
        {
            List<DayGraphRow> res = new List<DayGraphRow>();
            const byte segment = 45;                                //Сегмент суточного графика
            const uint LineLen = 28;                                //28 байт на 1 строку данных по протоколу счетчика
            const uint MaxReqLines = 8;                             //Максимальное количество строк в запросе
            const byte MaxReqBytes = (byte)(LineLen * MaxReqLines); //Максимальный размер запроса в байтах
            int TotalLines = GetDayGraphRecs();                     //Количество строк суточного графика фактически в счетчике
            //Если запрос выходит за диапазон или нет строк для чтения, то выход
            if ((TotalLines <= 0) || ((lines + offset) > TotalLines))
            {
                ChangeStatus(StringsTable.Timeout, false);
                SetProgress(100);
                return res;
            }
            uint OffsetBytes = (uint)(offset * LineLen);
            int BytesReq = (int)((lines - offset) * LineLen);                     //Всего запрошено байт
            ushort curroffs = 0;                                            //Текущий сдвиг в байтах
            ushort maxoffs = (ushort)(OffsetBytes + (lines - 1) * LineLen); //Максимальный сдвиг для чтения последней строки
            byte bytestoread = MaxReqBytes;                                 //Байт в запросе
            float Progress = 0;                                             //Прогресс выполнения операции
            float ProgressStep = (float)(100.0 / BytesReq);                 //Прогресс на 1 запрошенный байт
            while (curroffs <= maxoffs)
            {
                if (((BytesReq - curroffs) / MaxReqBytes) == 0)
                {
                    bytestoread = (byte)((BytesReq - curroffs) % MaxReqBytes);
                }

                if (SegmentRead(segment, curroffs, bytestoread, out byte[] QueryResult))
                {
                    Progress += ProgressStep * bytestoread;
                    SetProgress(Progress);                                                  //Вызов события прогресса
                    curroffs += bytestoread;
                    uint LinesRead = bytestoread / LineLen;                                 //Счетчик считанных строк
                    //Получение из ответа структуры суточного графика
                    for (int i = 0; i < LinesRead; i++)
                    {
                        res.Add(ToDayGraphRow(ArrayPart(QueryResult,(int)(i * LineLen), (int)LineLen)));
                    }
                }
                else { break; }
            }
            return res;
        }
        /// <summary>
        /// Получить суточный график  (сегмент #45)
        /// </summary>
        /// <returns></returns>
        public List<DayGraphRow> GetDayGraphList()
        {
            List<DayGraphRow> res = new List<DayGraphRow>();
            int recs = GetDayGraphRecs();
            if (recs > 0)
            {
                res = GetDayGraphList(recs, 0);
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
        /// Получить карту сегментов
        /// </summary>
        /// <returns>Список сегментов</returns>
        public List<SegmentsMapRec> GetSegmentsMap()
        {
            List<SegmentsMapRec> res = new List<SegmentsMapRec>();
            SegmentsMapRec item;
            //Получение количества сегментов
            const byte segment = 1;
            const int RecSize = 4;              //Количество байт на 1 запись 
            ushort offset = 0;
            byte len = 1;
            if (!SegmentRead(segment, offset, len, out byte[] QueryResult)) { return res; }
            int nsegment = QueryResult[0];      //Количество сегментов в таблице
            //Получение списка сегментов
            offset++;
            len = (byte)(nsegment * RecSize);
            if (!SegmentRead(segment, offset, len, out QueryResult)) { return res; }
            for (int i = 0; i < nsegment; i++)
            {
                item = ToSegmentsMapRec(ArrayPart(QueryResult, i * 4, RecSize));
                res.Add(item);
            }
            return res;
        }

        /// <summary>
        /// Возвращает текущее время счетчика в текущем часовом поясе
        /// </summary>
        /// <param name="Time">Текущее время счетчика</param>
        /// <returns>Успех операции</returns>
        public bool GetTime(out DateTime Time)
        {
            Time = default;
            const byte Segment = 64;
            const UInt16 Offset = 0;
            const byte Len = sizeof(UInt32);
            if (!SegmentRead(Segment, Offset, Len, out byte[] answ)) { return false; }
            Time = ToUTCTime(answ).ToLocalTime();
            return true;
        }

        //------------Методы для внутреннего использования-----------------------------------
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

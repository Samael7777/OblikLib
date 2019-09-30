//Операции с сегментами счетчика

using System;
using System.Collections.Generic;
using OblikControl.Resources;
using System.Text;


namespace OblikControl
{
    public partial class Oblik
    {
        /// <summary>
        /// Получить количество строк суточного графика (сегмент #44)
        /// </summary>
        /// <returns>Количество строк суточного графика</returns>
        public int GetDayGraphRecs()
        {
            const byte segment = 44;
            const ushort offset = 0;
            const byte len = 2;
            int res;
            byte[] QueryResult = null;
            try
            {
                SegmentRead(segment, offset, len, out QueryResult);
            }
            finally
            {
                res = (QueryResult == null) ? -1 : ToUint16(QueryResult);
                ChangeStatus(StringsTable.GetDGROK);
            }
            return res;
        }

        /// <summary>
        /// Очистка суточного графика (сегмент #88)
        /// </summary>
        public void CleanDayGraph()
        {
            const byte segment = 88;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_ConParams.Address);
            cmd[1] = (byte)_ConParams.Address;
            try
            {
                SegmentWrite(segment, offset, cmd);
            }
            finally
            {
                ChangeStatus(StringsTable.CleanDGOK);
            }    
        }

        /// <summary>
        /// Очистка протокола событий (сегмент #89)
        /// </summary>
        public void CleanEventsLog()
        {
            const byte segment = 89;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_ConParams.Address);
            cmd[1] = (byte)_ConParams.Address;
            try
            {
                SegmentWrite(segment, offset, cmd);
            }
            finally
            {
                ChangeStatus(StringsTable.CleanELOK);
            }
        }

        /// <summary>
        /// Установка текущего времени в счетчике (сегмент #65)
        /// </summary>
        public void SetCurrentTime()
        {
            const byte segment = 65;
            const ushort offset = 0;
            DateTime CurrentTime = DateTime.Now.ToUniversalTime();          //Текущее время в формате UTC
            CurrentTime.AddSeconds(2);                                      //2 секунды на вычисление, отправку и т.д.
            byte[] Buf = ToTime(CurrentTime);
            try
            {
                SegmentWrite(segment, offset, Buf);
            }
            finally
            {
                ChangeStatus(StringsTable.SetCurrTimeOK);
            }
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
            //Если запрос выходит за диапазон или нет строк для чтения, то исключение
            if ((TotalLines <= 0) || ((lines + offset) > TotalLines))
            {
                throw new OblikException(StringsTable.RangeErr);
            }
            uint OffsetBytes = (uint)(offset * LineLen);
            int BytesReq = (int)((lines - offset) * LineLen);               //Всего запрошено байт
            ushort curroffs = 0;                                            //Текущий сдвиг в байтах
            ushort maxoffs = (ushort)(OffsetBytes + (lines - 1) * LineLen); //Максимальный сдвиг для чтения последней строки
            byte bytestoread = MaxReqBytes;                                 //Байт в запросе
            float Progress = 0;                                             //Прогресс выполнения операции
            float ProgressStep = (float)(100.0 / BytesReq);                 //Прогресс на 1 запрошенный байт
            byte[] QueryResult = null;
            while (curroffs <= maxoffs)
            {
                if (((BytesReq - curroffs) / MaxReqBytes) == 0)
                {
                    bytestoread = (byte)((BytesReq - curroffs) % MaxReqBytes);
                }
                try
                {
                    SegmentRead(segment, curroffs, bytestoread, out QueryResult);
                }
                finally
                {
                    Progress += ProgressStep * bytestoread;
                    SetProgress(Progress);                                                  //Вызов события прогресса
                    curroffs += bytestoread;
                    uint LinesRead = bytestoread / LineLen;                                 //Счетчик считанных строк
                    //Получение из ответа структуры суточного графика
                    if (QueryResult != null)
                    {
                        for (int i = 0; i < LinesRead; i++)
                        {
                            res.Add(ToDayGraphRow(ArrayPart(QueryResult, (int)(i * LineLen), (int)LineLen)));
                        }
                    }  
                }
            }
            ChangeStatus(StringsTable.GetDayGraphListOK);
            return res;
        }
        /// <summary>
        /// Получить суточный график  (сегмент #45)
        /// </summary>
        /// <returns></returns>
        public List<DayGraphRow> GetDayGraphList()
        {
            List<DayGraphRow> res = new List<DayGraphRow>();
            try
            {
                int recs = GetDayGraphRecs();
                if (recs > 0)
                {
                    res = GetDayGraphList(recs, 0);
                }
            }
            finally { }
            return res;
        }

        /// <summary>
        /// Получить версию ПО счетчика (сегмент #2)
        /// </summary>
        /// <param name="FirmwareVer">Структура версии ПО</param>
        public void GetFWVersion(out FirmwareVer FirmwareVer)
        {
            FirmwareVer = new FirmwareVer();
            const byte segment = 2;
            const ushort offset = 0;
            const byte len = 2;
            byte[] QueryResult = null;
            try
            {
                SegmentRead(segment, offset, len, out QueryResult);
            }
            finally
            {
                if (QueryResult != null)
                {
                    FirmwareVer.Version = (int)QueryResult[0];
                    FirmwareVer.Build = (int)QueryResult[1];
                }
                ChangeStatus(StringsTable.GetFWVerOK);
            }
        }

        /// <summary>
        /// Получить текущие (усредненные за 2 сек.) значения электроэнергии (сегмент #36)
        /// </summary>
        /// <param name="values">Структура текущих значений</param>
        public void GetCurrentValues(out CurrentValues Values)
        {
            Values = new CurrentValues();
            const byte segment = 36;
            const UInt16 offset = 0;
            const byte len = 33;
            byte[] QueryResult = null;
            try
            {
                SegmentRead(segment, offset, len, out QueryResult);
            }
            finally
            {
                if (QueryResult != null)
                {
                    Values = ToCurrentValues(QueryResult);
                }
                ChangeStatus(StringsTable.GetCVOK);
            }
        }

        /// <summary>
        /// Получить карту сегментов
        /// </summary>
        /// <returns>Список сегментов</returns>
        public List<SegmentsMapRec> GetSegmentsMap()
        {
            List<SegmentsMapRec> res = new List<SegmentsMapRec>();
            SegmentsMapRec item;
            const byte segment = 1;
            const int RecSize = 4;              //Количество байт на 1 запись 
            ushort offset = 0;
            byte len = 1;
            byte[] QueryResult = null;
            int nsegment = 0;                   //Количество сегментов в таблице
            try
            {
                //Получение количества сегментов
                SegmentRead(segment, offset, len, out QueryResult);
                nsegment = QueryResult[0];      
                //Получение списка сегментов
                offset++;
                len = (byte)(nsegment * RecSize);
                SegmentRead(segment, offset, len, out QueryResult);
            }
            finally
            {
                for (int i = 0; i < nsegment; i++)
                {
                    item = ToSegmentsMapRec(ArrayPart(QueryResult, i * 4, RecSize));
                    res.Add(item);
                }
            }
            ChangeStatus(StringsTable.GetSegMapOK);
            return res;
        }

        /// <summary>
        /// Возвращает текущее время счетчика в текущем часовом поясе
        /// </summary>
        /// <returns>Текущее время счетчика в текущем часовом поясе</returns>
        public DateTime GetTime()
        {
            const byte Segment = 64;
            const UInt16 Offset = 0;
            const byte Len = sizeof(UInt32);
            byte[] answ = null;
            DateTime res = default;
            try
            {
                SegmentRead(Segment, Offset, Len, out answ);
            }
            finally
            {
                ChangeStatus(StringsTable.GetTimeOK);
            }
            if (answ != null)
            {
                res = ToUTCTime(answ).ToLocalTime();
            }
            return res;
        }

        /// <summary>
        /// Получить настройки сети счетчика (сегмент #66)
        /// </summary>
        /// <returns>настройки сети счетчика</returns>
        public NetworkConfig GetNetworkConfig()
        {
            const byte Segment = 66;
            const UInt16 Offset = 0;
            const byte Len = 3;
            byte[] answ = null;
            NetworkConfig res = default;
            try
            {
                SegmentRead(Segment, Offset, Len, out answ);
            }
            finally
            {
                if (answ != null)
                {
                    res.addr = answ[0];
                    res.divisor = ToUint16(ArrayPart(answ, 1, 2));
                }
            }
            ChangeStatus(StringsTable.GetNetOk);
            return res;
        }

        /// <summary>
        /// Установить настройки сети счетчика (сегмент #67)
        /// </summary>
        /// <param name="nc">Настройки сети счетчика</param>
        public void SetNetworkConfig (NetworkConfig nc)
        {
            const byte Segment = 67;
            const UInt16 Offset = 0;
            byte[] Buf = NetworkConfigToByte(nc);
            try
            {
                SegmentWrite(Segment, Offset, Buf);
            }
            finally
            {
                //Меняем настройку сети класса в соответствии с новой настройкой
                if (_ConParams.Address != 0)
                {
                    _ConParams.Address = nc.addr;
                    _ConParams.Baudrate = 115200 / nc.divisor;
                }
                ChangeStatus(StringsTable.SetNetOK);
            }
        }
        
        /// <summary>
        /// Установка пароля пользователя
        /// </summary>
        /// <param name="accessLevel">Уровень доступа</param>
        /// <param name="password">Пароль</param>
        public void SetPassword(AccessLevel accessLevel, string password)                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
        {
            if (password == null) { password = string.Empty; }
            if (password.Length > 8) { password = password.Substring(0, 8); }
            if (password.Length < 8) { password += new string(' ', 8 - password.Length); }
            byte[] pwdarray = Encoding.Default.GetBytes(password);
            const ushort Offset = 0;
            byte Segment = 0;
            switch (accessLevel)
            {
                case AccessLevel.User:
                    Segment = 72;
                    break;
                case AccessLevel.Admin:
                    Segment = 73;
                    break;
                case AccessLevel.Energo:
                    Segment = 74;
                    break;
                case AccessLevel.System:
                    //Исключение при попытке поменять пароль системному пользователю
                    ChangeStatus(StringsTable.PwdSetUserErr);
                    throw new OblikException(StringsTable.PwdSetUserErr);
            }
            try
            {
                SegmentWrite(Segment, Offset, pwdarray);
            }
            finally
            {
                ChangeStatus(StringsTable.PwdSetOK);
                if (_ConParams.AccessLevel == accessLevel)
                {
                    _ConParams.Password = password;
                }
            }
        }
        
        
        
        //------------Методы для внутреннего использования-----------------------------------
        /// <summary>
        /// Получить параметры вычислений из счетчика (сегмент #56)
        /// </summary>
        private void GetCalcUnits()
        {
            byte segment = 56;                                      //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte len = 57;
            byte[] QueryResult = null;
            try
            {
                SegmentRead(segment, offset, len, out QueryResult);
            }
            finally
            {
                if (QueryResult != null)
                {
                    _CalcUnits = ToCalcUnits(QueryResult);
                }
            }
        }

        /// <summary>
        /// //Записать параметры вычислений из свойства объекта в счетчик (сегмент #57)
        /// </summary>
        /// <returns>Успех операции</returns>
        private void SetCalcUnits()
        {
            byte segment = 57;                                     //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte[] data = CalcUnitsToByte(_CalcUnits);
            try
            {
                SegmentWrite(segment, offset, data);
            }
            finally { }
        }
    }
}

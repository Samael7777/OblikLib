using System;
using System.Collections.Generic;


namespace Oblik
{
    public partial class Oblik : IOblik
    {
        
        public int GetDayGraphRecs()
        {
            const byte segment = 44;
            const ushort offset = 0;
            SegmentAccsess(segment, offset, 2, null, 0);
            //Порядок байт в счетчике - обратный по отношению к пк, переворачиваем
            if (!_isError)
            {
                return (int)(L2Data[0] + (int)(L2Data[1] << 8));
            }
            else return -1;
        }
        public bool CleanDayGraph()
        {
            const byte segment = 88;
            const ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_addr);
            cmd[1] = (byte)_addr;
            SegmentAccsess(segment, offset, (byte)cmd.Length, cmd, 1);
            return _isError;
        }
        public bool SetCurrentTime()
        {
            DateTime CurrentTime = System.DateTime.Now.ToUniversalTime();        //Текущее время в формате UTC
            CurrentTime.AddSeconds(2);                                           //2 секунды на вычисление, отправку и т.д.
            byte[] Buf = ToTime(CurrentTime);
            SegmentAccsess(65, 0, (byte)Buf.Length, Buf, 1);
            return _isError;
        }
        public List<DayGraphRow> GetDayGraph(uint lines, uint offset)   //Получение суточного графика: lines - количество строк, offset - смещение (в строках)
        {
            List<DayGraphRow> res = new List<DayGraphRow>();
            const byte segment = 45;                                //Сегмент суточного графика
            const uint LineLen = 28;                                //28 байт на 1 строку данных по протоколу счетчика
            const uint MaxReqLines = 8;                             //Максимальное количество строк в запросе
            const byte MaxReqBytes = (byte)(LineLen * MaxReqLines); //Максимальный размер запроса в байтах
            byte[] _buf;                                            //Буфер
            uint TotalLines = (uint)GetDayGraphRecs();              //Количество строк суточного графика фактически в счетчике
            if (TotalLines == 0)
            {
                ChangeStatus("Нет записей для чтения", false);
                return res;
            }
            if (_isError) { return res; }

            if ((lines + offset) > TotalLines)                      //Если запрос выходит за диапазон, запросить только последнюю строку
            {
                lines = 1;
                offset = TotalLines - 1;
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
                SegmentAccsess(segment, curroffs, bytestoread, null, 0);
                if (_isError) { break; }                                    //Выход из цикла при ошибке
                Progress += ProgressStep * bytestoread;
                SetProgress(Progress);                                      //Вызов события прогресса
                Array.Resize(ref _buf, (int)(curroffs + LineLen));
                Array.Copy(L2Data, 0, _buf, curroffs, L2Data.Length);       //Результат считывания помещается в L2Data
                curroffs += bytestoread;
                LinesRead += bytestoread / LineLen;
            }
            //Получение из ответа структуры суточного графика
            for (int i = 0; i < LinesRead; i++)
            {
                byte[] _tmp = new byte[LineLen];
                Array.Copy(_buf, (i * LineLen), _tmp, 0, LineLen);
                res.Add(ToDayGraphRow(_tmp));
            }
            return res;
        }
        public bool GetFWVersion(out FirmwareVer fw)                    //Получить версию ПО счетчика
        {
            fw = new FirmwareVer();
            const byte segment = 44;
            const ushort offset = 0;
            const byte len = 2;
            SegmentAccsess(segment, offset, len, null, 0);
            if (!IsError)
            {
                fw.Version = (int)L2Data[0];
                fw.Build = (int)L2Data[1];
            }
            return _isError;
        }
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
        private void GetCalcUnits()                                //Получить параметры вычислений
        {
            byte segment = 56;                                      //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte len = 57;                                          //Размер данных сегмента
            SegmentAccsess(segment, offset, len, null, 0);
            if (!_isError || (L2Data.Length != 57)) { return; }
            _CalcUnits = ToCalcUnits(L2Data);
        }
        private void SetCalcUnits()                                //Записать параметры вычислений
        {
            byte segment = 56;                                     //Сегмент чтения параметров вычислений
            UInt16 offset = 0;
            byte[] data = CalcUnitsToByte(_CalcUnits);
            SegmentAccsess(segment, offset, (byte)data.Length, data, 1);
        }

    }
}

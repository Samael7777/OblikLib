using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;



namespace Oblik
{
    
    //Типы событий
    public class ProgressEventArgs : EventArgs { public float progress; };  //Аргументы события прогресса данных
    public class ErrEventArgs : EventArgs { public string Message; };       //Аргументы события ошибки
    public class StatusChangeArgs : EventArgs { public string Message; };   //Аргументы события изменения статуса
    //public class DataRecievedArgs: EventArgs { public object Data; };       //Аргументы события получения данных

    //Структуры данных
    public struct DayGraphRow                                               //Структура строки суточного графика
    {
        public DateTime time;       //время записи
        public float act_en_p;      //активная энергия "+" за период сохранения 
        public float act_en_n;      //активная энергия "-" за период сохранения
        public float rea_en_p;      //реактивная энергия "+" за период сохранения
        public float rea_en_n;      //реактивная энергия "-" за период сохранения
        public ushort[] channel;    //Количество импульсов по каналам
    }
    public struct CalcUnitsStruct                                           //Структура параметров вычислений
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
    
    //Класс счетчиков Облик
    public class Oblik
    {
        //Локальные переменные
        private readonly int _port;                     //порт счетчика
        private readonly int _addr;                     //адрес счетчика
        private readonly int _baudrate;                 //скорость работы порта, 9600 бод - по умолчанию
        private int _timeout, _repeats;                 //таймаут и повторы
        private byte[] _passwd;                         //пароль
        private bool _isError;                          //Наличие ошибки
        private byte _user;                             //Пользователь от 0 до 3 (3 - максимальные привелегии, 0 - минимальные)
        private readonly object SerialIncoming;         //Монитор таймаута чтения порта
        private CalcUnitsStruct _CalcUnits;             //Параметры вычислений  

        //Интерфейс класса
        //Делегаты событий класса
        public delegate void Progress(object sender, ProgressEventArgs e);      
        public delegate void Error(object sender, ErrEventArgs e);
        public delegate void StatusChange(object sender, StatusChangeArgs e);
        //public delegate void DataRecieved(object sender, DataRecievedArgs e);
        
        //События класса
        public event Progress OnProgress;               //Событие прогресса данных
        public event Error OnError;                     //Событие ошибки
        public event StatusChange OnStatusChange;       //Событие изменения статуса
        //public event DataRecieved OnDataRecieved;       //Событие получения данных TODO!!!!!

        //Структура ответа счетчика
        public int L1Result { get; set; }                //Результат фрейма L1
        public string L1ResultMsg { get; set; }          //Результат фрейма L1, расшифровка
        public int L1Lenght { get; set; }                //Количетво байт в полях "Длина", "L2Data", "Результат" 
        public int L1Sum { get; set; }                   //Контрольная сумма
        public int L2Result { get; set; }                //Результат запроса L2
        public string L2ResultMsg { get; set; }          //Результат запроса L2, расшифровка
        public int L2Lenght { get; set; }                //Количество данных, успешно обработанных операцией
        public byte[] L2Data { get; set; }               //Данные L2
        //Свойства
        public int Repeats
        {
            set => _repeats = value;
            get => _repeats;
        }                           //Количество повторов передачи
        public int Timeout
        {
            set => _timeout = value;
            get => _timeout;
        }                           //Таймаут соединения
        public bool IsError
        {
            set => _isError = value;
            get => _isError;
        }                          //Индикатор наличия ошибки
        public string Password
        {
            set => _passwd = Encoding.Default.GetBytes(value);
            get => Encoding.Default.GetString(_passwd);
        }                       //Пароль счетчика
        public int User
        {
            set => _user = (byte)value;
            get => _user;
        }                              //Пользователь
        public CalcUnitsStruct CalcUnits                //Параметры вычислений
        {
            get
            {
                GetCalcUnits();
                return _CalcUnits;
            }
            set
            {
                _CalcUnits = value;
                SetCalcUnits();
            }
        }
        public Oblik(int port, int baudrate, int addr, int timeout, int repeats, string password)
        {
            _port = port;
            _addr = addr;
            _timeout = timeout;
            _repeats = repeats;
            _baudrate = baudrate;
            _passwd = new byte[8];
            _isError = false;
            if (password == "")
            {
                for (int i = 0; i < 8; i++) { _passwd[i] = 0; }
            }
            else
            {
                _passwd = Encoding.Default.GetBytes(password);
            }
            _user = 2;
            SerialIncoming = new object();
            _CalcUnits = new CalcUnitsStruct();
        }
        public Oblik(int port, int addr, int timeout, int repeats) : this(port, 9600, addr, timeout, repeats, "") { }
        public Oblik(int port, int addr) : this(port, 9600, addr, 500, 2, "") { }

        //Реализация

        /*
        Доступ к данным в сегменте счетчика, возвращает количество прочитанных байт ответа счетчика или -1 в случае ошибки
        data - данные для записи в сегмент, если запрос на чтение, то null
        len - количество байт для записи / чтения
        answ - полученные данные со счетчика (не обработанные)
        AccType: 0 - чтение, 1 - запись 
        */
        public void SegmentAccsess(byte segment, UInt16 offset, byte len, byte[] data, byte AccType)
        {
            byte[] _l1;                                                     //Посылка 1 уровня
            byte[] _l2;                                                     //Посылка 2 уровня
            if (AccType != 0) { AccType = 1; }                              //Все, что больше 0 - команда на запись

            //Формируем запрос L2
            _l2 = new byte[5 + (len + 8) * AccType];                        //5 байт заголовка + 8 байт пароля + данные 
            _l2[0] = (byte)((segment & 127) + AccType * 128);               //(биты 0 - 6 - номер сегмента, бит 7 = 1 - операция записи)
            _l2[1] = _user;                                                 //Указываем пользователя
            _l2[2] = (byte)(offset >> 8);                                   //Старший байт смещения
            _l2[3] = (byte)(offset & 0xff);                                 //Младший байт смещения
            _l2[4] = len;                                                   //Размер считываемых данных

            //Если команда - на запись в сегмент
            if (AccType == 1)
            {
                Array.Copy(data, 0, _l2, 5, len);                               //Копируем данные в L2
                Array.Copy(_passwd, 0, _l2, len + 5, 8);                        //Копируем пароль в L2
                Encode(ref _l2);                                                //Шифруем данные и пароль L2
            }

            //Формируем фрейм L1
            _l1 = new byte[5 + _l2.Length];
            _l1[0] = 0xA5;                              //Заголовок пакета
            _l1[1] = 0x5A;                              //Заголовок пакета
            _l1[2] = (byte)(_addr & 0xff);              //Адрес счетчика
            _l1[3] = (byte)(3 + _l2.Length);            //Длина пакета L1 без ключей
            Array.Copy(_l2, 0, _l1, 4, _l2.Length);     //Вставляем запрос L2 в пакет L1

            //Вычисление контрольной суммы, побайтовое XOR, от поля "Адрес" до поля "L2"
            _l1[_l1.Length - 1] = 0;
            for (int i = 2; i < (_l1.Length - 1); i++)
            {
                _l1[_l1.Length - 1] ^= (byte)_l1[i];
            }

            //Обмен данными со счетчиком
            byte[] answer = new byte[0];
            OblikQuery(_l1, ref answer);

            //Заполняем структуру ответа счетчика
            if (!_isError)
            {
                AnswerParser(answer);
            }

        }
        public int GetDayGraphRecs()                                //Получить количество записей суточного графика
        {
            SegmentAccsess(44, 0, 2, null, 0);
            //Порядок байт в счетчике - обратный по отношению к пк, переворачиваем
            if (!_isError)
            {
                return (int)(L2Data[0] + (int)(L2Data[1] << 8));
            }
            else return -1;
        }
        public void CleanDayGraph()                                 //Стирание суточного графика
        {
            byte segment = 88;
            ushort offset = 0;
            byte[] cmd = new byte[2];
            cmd[0] = (byte)~(_addr);
            cmd[1] = (byte)_addr;
            SegmentAccsess(segment, offset, (byte)cmd.Length, cmd, 1);
        }
        public void SetCurrentTime()                                //Установка текущего времени в счетчике
        {
            DateTime CurrentTime = System.DateTime.Now.ToUniversalTime();        //Текущее время в формате UTC
            CurrentTime.AddSeconds(2);                                           //2 секунды на вычисление, отправку и т.д.
            byte[] Buf = ToTime(CurrentTime); 
            SegmentAccsess(65, 0, (byte)Buf.Length, Buf, 1);
        }
        public Nullable<DayGraphRow>[] GetDayGraph(uint lines, uint offset)   //Получение суточного графика: lines - количество строк, offset - смещение (в строках)
        {
            Nullable <DayGraphRow>[] res;
            const byte segment = 45;                                //Сегмент суточного графика
            const uint LineLen = 28;                                //28 байт на 1 строку данных по протоколу счетчика
            const uint MaxReqLines = 8;                             //Максимальное количество строк в запросе
            const byte MaxReqBytes = (byte)(LineLen * MaxReqLines); //Максимальный размер запроса в байтах
            byte[] _buf;                                            //Буфер
            uint TotalLines = (uint)GetDayGraphRecs();              //Количество строк суточного графика фактически в счетчике
            if (_isError || (TotalLines == 0)) { return null; }      //Возврат null в случае ошибки или отсутствия записей для считывания
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
            res = new Nullable<DayGraphRow>[LinesRead];
            for (int i = 0; i < LinesRead; i++)
            {
                byte[] _tmp = new byte[LineLen];
                Array.Copy(_buf, (i * LineLen), _tmp, 0, LineLen);
                res[i] = ToDayGraphRow(_tmp);
            }
            return res;
        }

        //Вспомогательные функции для внутреннего использования
        private void GetCalcUnits()                                 //Получить параметры вычислений
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
        private string ParseL1error(int error)                     //Парсер ошибок L1
        {
            string res;
            switch (error)
            {
                case 1:
                    res = "Успешное выполнение запроса";
                    break;
                case 0xff:
                    res = "Ошибка контрольной суммы";
                    break;
                case 0xfe:
                    res = "Переполнение входного буфера счетчика";
                    break;
                default:
                    res = "Неизвестная ошибка";
                    break;
            }
            return res;
        }
        private string ParseL2error(int error)                     //Парсер ошибок L2
        {
            string res;
            switch (error)
            {
                case 0:
                    res = "Успешное выполнение операции";
                    break;
                case 0xff:
                    res = "Некорректный запрос (содержит менее 5 байт)";
                    break;
                case 0xfe:
                    res = "Неправильный идентификатор сегмента";
                    break;
                case 0xfd:
                    res = "Некорректная операция (Попытка записи в сегмент чтения и наоборот)";
                    break;
                case 0xfc:
                    res = "Неправильно задан уровень пользователя";
                    break;
                case 0xfb:
                    res = "Нет права доступа к данным";
                    break;
                case 0xfa:
                    res = "Неправильно задано смещение";
                    break;
                case 0xf9:
                    res = "Неправильный запрос на запись (несоответствие запрашиваемой и действительной длины данных)";
                    break;
                case 0xf8:
                    res = "Длина данных задана равной 0";
                    break;
                case 0xf7:
                    res = "Неправильный пароль";
                    break;
                case 0xf6:
                    res = "Неправильно задана команда стирания графиков";
                    break;
                case 0xf5:
                    res = "Запрещена смена пароля";
                    break;
                default:
                    res = "Неизвестная ошибка";
                    break;
            }
            return res;
        }
        private void OblikQuery(byte[] Query, ref byte[] Answer)   //Отправка запроса и получение данных Query - запрос, Answer - ответ
        {
            _isError = false;
            byte[] _rbuf = new byte[0];                   //Буфер для чтения
            //Параметризация и открытие порта
            using (SerialPort com = new SerialPort
                {
                    PortName = "COM" + _port,
                    BaudRate = _baudrate,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    ReadTimeout = _timeout,
                    WriteTimeout = _timeout,
                    DtrEnable = false,
                    RtsEnable = false,
                    Handshake = Handshake.None
                })
            {
                // Событие чтения данных из порта
                void DataReciever(object s, SerialDataReceivedEventArgs ea)
                {
                    Array.Resize(ref _rbuf, com.BytesToRead);
                    com.Read(_rbuf, 0, _rbuf.Length);
                    lock (SerialIncoming)
                    {
                        Monitor.Pulse(SerialIncoming);
                    }
                }
                try
                {
                    if (com.IsOpen) { com.Close(); }    //закрыть ранее открытый порт
                    com.Open();
                    //Отправка данных
                    com.DiscardOutBuffer();                                                                 //очистка буфера передачи
                    ChangeStatus("Отправка запроса");
                    com.DataReceived += new SerialDataReceivedEventHandler(DataReciever);                   //событие чтения из порта
                    com.Write(Query, 0, Query.Length);                                                      //отправка буфера записи
                    com.DiscardInBuffer();                                                                  //очистка буфера приема
                    //Получение ответа
                    int r = _repeats;
                    bool ReadOk = false;
                    ChangeStatus("Ожидание ответа...");
                    while (r > 0)   //Повтор при ошибке
                    {
                        lock (SerialIncoming)
                        {
                            if (!Monitor.Wait(SerialIncoming, _timeout))
                            {
                                //Если таймаут
                                ChangeStatus("Timeout");
                                r--;
                            }
                            else
                            {
                                r = 0;
                                ReadOk = true;
                                ChangeStatus("Данные получены");
                            }
                        }
                    }
                    if (!ReadOk) { RaiseError("Нет данных"); }
                    com.Close();        //Закрыть порт
                    if (!_isError)
                    {
                        Array.Resize(ref Answer, _rbuf.Length);
                        Array.Copy(_rbuf, 0, Answer, 0, _rbuf.Length);
                    }
                }
                catch (Exception e)
                {
                    RaiseError(e.Message);
                }
                finally
                {
                    Query = null;
                }
            }
        }
        private void Encode(ref byte[] l2)                         //Процедура шифрования данных L2
        {
            //Шифрование полей "Данные" и "Пароль". Сперто из оригинальной процедуры шифрования
            byte _x1 = 0x3A;
            for (int i = 0; i <= 7; i++) { _x1 ^= _passwd[i]; }
            byte _dpcsize = (byte)(l2[4] + 8);                                //Размер "Данные + "Пароль" 
            int k = 4;
            for (int i = _dpcsize - 1; i >= 0; i--)
            {
                byte _x2 = l2[k++];
                l2[k] ^= _x1;
                l2[k] ^= _x2;
                l2[k] ^= _passwd[i % 8];
                _x1 += (byte)i;
            }
        }
        private void AnswerParser(byte[] answer)                   //Парсер ответа счетчика
        {
            L1Result = answer[0];
            L1ResultMsg = ParseL1error(L1Result);
            ChangeStatus(L1ResultMsg);
            if (L1Result == 1)
            {
                L1Lenght = answer[1];
                L1Sum = answer[answer.Length - 1];
                L2Result = answer[2];
                L2ResultMsg = ParseL2error(L2Result);
                L2Lenght = answer[3];
                if (L2Result == 0)
                {
                    L2Data = new byte[L1Lenght - 2];
                    Array.Copy(answer, 4, L2Data, 0, answer.Length - 5);
                }
                else
                {
                    ChangeStatus(L2ResultMsg);
                    RaiseError(L2ResultMsg);
                }
                //Проверка контрольной суммы
                byte cs = 0;
                for (int i = 0; i < answer.Length; i++)
                {
                    cs ^= answer[i];
                }
                if (cs != 0)
                {
                    RaiseError("Ошибка контрольной суммы");
                    ChangeStatus("Ошибка контрольной суммы");
                }
                else
                {
                    _isError = false;
                    ChangeStatus(L2ResultMsg);
                }
            }
        }
        
        //Генераторы событий
        private void RaiseError(string message)                    //Вызов события ошибки 
        {
            ErrEventArgs args = new ErrEventArgs();
            args.Message = message;
            _isError = true;
            if (OnError != null)
            {
                OnError(this, args);
            }
        }
        private void ChangeStatus(string message)                  //Вызов события изменения статуса
        {
            StatusChangeArgs args = new StatusChangeArgs();
            args.Message = message;
            if (OnStatusChange != null)
            {
                OnStatusChange(this, args);
            }
        }
        private void SetProgress(float progress)                   //Вызов события прогресса
        {
            ProgressEventArgs args = new ProgressEventArgs();
            args.progress = progress;
            if (OnProgress != null)
            {
                OnProgress(this, args);
            }
            
        }
        
        //Группа преобразователей массива байт в различные типы данных и наоборот. 
        //Принимается, что старший байт имеет младший адрес (big-endian)
        private UInt32 ToUint32(byte[] array)                      //Преобразование массива байт в UInt32 
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    return reader.ReadUInt32();
                }
            }
            finally
            {
                if (stream != null )
                {
                    stream.Dispose();
                }
            }
        }
        private byte[] UInt32ToByte(UInt32 data)                   //Преобразование UInt32 в массив байт
        {
            byte[] res = new byte[sizeof(UInt32)];
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(res);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
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
        private float ToFloat(byte[] array)                        //Преобразование массива байт в float
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
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
        private byte[] FloatToByte (float data)                    //Преобразование float в массив байт
        {
            byte[] res = new byte[sizeof(float)];
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(res);
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
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
        private UInt16 ToUint16(byte[] array)                      //Преобразование массива байт в word (оно же uint16)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(array);
                using (BinaryReader reader = new BinaryReader(stream))
                {
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
        private byte[] UInt16ToByte(UInt16 data)                   //Преобразование word(UInt16) в массив байт
        {
            byte[] res = new byte[2];
            res[0] = (byte)((data & 0xFF00) >> 8);
            res[1] = (byte)(data & 0x00FF);
            return res;
        }
        private DateTime ToUTCTime(byte[] array)                   //Преобразование массива байт в дату и время
        {
            UInt32 _ctime;  //Время по стандарту t_time
            DateTime BaseTime, Time;
            BaseTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);       //Базовая точка времени 01.01.1970 00:00 GMT
            _ctime = ToUint32(array);                                             //Время в формате C (time_t) 
            Time = BaseTime.AddSeconds(_ctime);
            return Time;
        }
        private float ToUminiflo(byte[] array)                     //Преобразование массива байт в uminiflo 
        {
            UInt16 _data = ToUint16(array);
            UInt16 man, exp;
            float res;
            man = (UInt16)(_data & 0x7FF);                                      //Мантисса - биты 0-10
            exp = (UInt16)((_data & 0xF800) >> 11);                             //Порядок - биты 11-15
            res = (float)System.Math.Pow(2, (exp - 15)) * (1 + man / 2048);     //Pow - возведение в степень
            return res;
        }
        private float ToSminiflo(byte[] array)                     //Преобразование массива байт в sminiflo
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
        private Nullable<DayGraphRow> ToDayGraphRow(byte[] array)  //Преобразование массива байт в строку суточного графика
        {
            try
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
            catch (Exception e)
            {
                RaiseError(e.Message);
                return null;
            }
        }
        private byte[] ToTime (DateTime Date)                      //Преобразование DateTime в массив байт согласно t_time 
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
        private CalcUnitsStruct ToCalcUnits(byte[] array)          //Преобразование массива байт в структуру параметров вычислений
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
        private byte[] CalcUnitsToByte(CalcUnitsStruct CalcUnits)  //Преобразование структуры параметров вычислений в массив байт
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
    }   
}
//Обеспечение операций ввода/вывода

using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Oblik
{
    public partial class Oblik
    {

        /// <summary>
        /// Тип доступа к сегменту
        /// Write - на запись,
        /// Read - на чтение
        /// </summary>
        enum Access : byte
        {
            Write = 1,
            Read = 0
        }

        /// <summary>
        /// Отправка запроса к счетчику и получение данных
        /// </summary>
        /// <param name="Query">Запрос к счетчику в формате массива L1</param>
        /// <param name="Answer">Ответ счетчика в формате массива L1</param>
        /// <return>Успех</return>>
        private bool OblikQuery(byte[] Query, out byte[] Answer)
        {
            bool success = true;                          //Флаг успеха операции
            object SerialIncoming = new object();         //Монитор таймаута чтения порта
            Answer = new byte[0];
            byte[] _rbuf = new byte[0];                   //Буфер для чтения
            //Параметризация и открытие порта
            using (SerialPort com = new SerialPort
            {
                PortName = "COM" + _ConParams.Port.ToString(),
                BaudRate = _ConParams.Baudrate.Value,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = _ConParams.Timeout.Value,
                WriteTimeout = _ConParams.Timeout.Value,
                DtrEnable = false,
                RtsEnable = false,
                Handshake = Handshake.None
            })
            {
                // Метод события чтения данных из порта
                void DataReciever(object sender, SerialDataReceivedEventArgs ea)
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
                    ChangeStatus("Отправка запроса", false);
                    com.DataReceived += new SerialDataReceivedEventHandler(DataReciever);                   //событие чтения из порта
                    com.Write(Query, 0, Query.Length);                                                      //отправка буфера записи
                    com.DiscardInBuffer();                                                                  //очистка буфера приема

                    //Получение ответа
                    int r = _ConParams.Repeats.Value;
                    bool ReadOk = false;
                    ChangeStatus("Ожидание ответа...", false);
                    while (r > 0)   //Повтор при ошибке
                    {
                        lock (SerialIncoming)
                        {
                            if (!Monitor.Wait(SerialIncoming, _ConParams.Timeout.Value))
                            {
                                //Если таймаут
                                ChangeStatus("Timeout", false);
                                r--;
                            }
                            else
                            {
                                r = 0;
                                ReadOk = true;
                                ChangeStatus("Данные получены", false);
                            }
                        }
                    }
                    if (!ReadOk)
                    {
                        success = false;
                        ChangeStatus("Нет данных", true);
                    }
                    com.Close();        //Закрыть порт
                    if (success)
                    {
                        Array.Resize(ref Answer, _rbuf.Length);
                        Array.Copy(_rbuf, 0, Answer, 0, _rbuf.Length);
                    }
                }
                catch (Exception e)
                {
                    ChangeStatus(e.Message, true);
                    return false;
                }
                finally
                {
                    Query = null;
                }
                return success;
            }
        }

        /// <summary>
        /// Подготовка фрейма запроса к счетчику
        /// </summary>
        /// <param name="Segment">Сегмент счетчика</param>
        /// <param name="Offset">Смещение относительно начала сегмента</param>
        /// <param name="Len">Количество данных для чтения/записи</param>
        /// <param name="Data">Данные для записи</param>
        /// <param name="Access">Доступ на запись или чтение</param>
        /// <returns></returns>
        private byte[] PerformHeaders(byte Segment, UInt16 Offset, byte Len, Access Access, byte[] Data = null)
        {
            byte[] _l1;                                                     //Посылка 1 уровня
            byte[] _l2;                                                     //Посылка 2 уровня
            byte[] _pwdarray = new byte[8];                                 //Пароль к счетчику
            for (int i = 0; i < 8; i++) { _pwdarray[i] = 0; }
            _pwdarray = Encoding.Default.GetBytes(_ConParams.Password);

            //Формируем запрос L2
            _l2 = new byte[5 + (Len + 8) * (int)Access];                    //5 байт заголовка + 8 байт пароля + данные 
            _l2[0] = (byte)((Segment & 127) + (int)Access * 128);           //(биты 0 - 6 - номер сегмента, бит 7 = 1 - операция записи)
            _l2[1] = (byte)_ConParams.AccessLevel.Value;                    //Указываем уровень доступа
            _l2[2] = (byte)(Offset >> 8);                                   //Старший байт смещения
            _l2[3] = (byte)(Offset & 0xff);                                 //Младший байт смещения
            _l2[4] = Len;                                                   //Размер считываемых данных

            if (Access == Access.Write)
            {
                Array.Copy(Data, 0, _l2, 5, Len);                           //Копируем данные в L2
                Array.Copy(_pwdarray, 0, _l2, Len + 5, 8);                  //Копируем пароль в L2
                Encode(ref _l2, _pwdarray);                                 //Шифруем данные и пароль L2
            }

            //Формируем фрейм L1
            _l1 = new byte[5 + _l2.Length];
            _l1[0] = 0xA5;                                                  //Заголовок пакета
            _l1[1] = 0x5A;                                                  //Заголовок пакета
            _l1[2] = (byte)(_ConParams.Address.Value & 0xff);               //Адрес счетчика
            _l1[3] = (byte)(3 + _l2.Length);                                //Длина пакета L1 без ключей
            Array.Copy(_l2, 0, _l1, 4, _l2.Length);                         //Вставляем запрос L2 в пакет L1

            //Вычисление контрольной суммы, побайтовое XOR, от поля "Адрес" до поля "L2"
            _l1[_l1.Length - 1] = 0;
            for (int i = 2; i < (_l1.Length - 1); i++)
            {
                _l1[_l1.Length - 1] ^= (byte)_l1[i];
            }

            return _l1;
        }

        /// <summary>
        /// Чтение сегмента счетчика
        /// </summary>
        /// <param name="Segment">Сегмент счетчика</param>
        /// <param name="Offset">Смещение относительно начала сегмента</param>
        /// <param name="Len">Количество данных для чтения</param>
        /// <param name="Data">Полученные данные</param>
        /// <returns>Успех операции</returns>
        public bool SegmentRead(byte Segment, UInt16 Offset, byte Len, out byte[] Data)
        {
            Data = null;
            byte[] answer;
            byte[] Query = PerformHeaders(Segment, Offset, Len, Access.Read);
            if (!OblikQuery(Query, out answer)) { return false; }
            if (!CheckAnswer(answer)) { return false; }
            Data = new byte[(answer.Length - 5)];
            Array.Copy(answer, 4, Data, 0, Data.Length);
            return true;
        }

        /// <summary>
        /// Запись в сегмент счетчика
        /// </summary>
        /// <param name="Segment">Сегмент счетчика</param>
        /// <param name="Offset">Смещение относительно начала сегмента</param>
        /// <param name="Data">Данные для записи</param>
        /// <returns>Успех операции</returns>
        public bool SegmentWrite(byte Segment, UInt16 Offset, byte[] Data)
        {
            byte[] answer;
            byte[] Query = PerformHeaders(Segment, Offset, (byte)Data.Length, Access.Write, Data);
            if (!OblikQuery(Query, out answer)) { return false; }
            if (!CheckAnswer(answer)) { return false; }
            return true;
        }


        //Методы обработки фреймов L1, L2

        /// <summary>
        /// Парсер ошибок L1
        /// </summary>
        /// <param name="error">Код ошибки L1</param>
        /// <returns>Строка с текстом ошибки</returns>
        private string ParseL1error(int error)
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

        /// <summary>
        /// Парсер ошибок L2
        /// </summary>
        /// <param name="error">Код ошибки L2</param>
        /// <returns>Строка с текстом ошибки</returns>
        private string ParseL2error(int error)
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


        /// <summary>
        /// Процедура шифрования данных L2
        /// </summary>
        /// <param name="l2">Ссылка на массив L2</param>
        /// <param name="passwd">Пароль</param>
        private void Encode(ref byte[] l2, byte[] passwd)
        {
            //Шифрование полей "Данные" и "Пароль". Сперто из оригинальной процедуры шифрования
            byte _x1 = 0x3A;
            for (int i = 0; i <= 7; i++) { _x1 ^= passwd[i]; }
            byte _dpcsize = (byte)(l2[4] + 8);                                //Размер "Данные + "Пароль" 
            int k = 4;
            for (int i = _dpcsize - 1; i >= 0; i--)
            {
                byte _x2 = l2[k++];
                l2[k] ^= _x1;
                l2[k] ^= _x2;
                l2[k] ^= passwd[i % 8];
                _x1 += (byte)i;
            }
        }

        /// <summary>
        /// Проверка принятых данных на корректность
        /// </summary>
        /// <param name="answer">Массив с ответом счетчика</param>
        /// <param name="QueryResult">Ответ счетчика без заголовков</param>
        /// <return>Корректность принятых данных</return>
        private bool CheckAnswer(byte[] answer)
        {
            int L1Result = answer[0];
            string L1ResultMsg = ParseL1error(L1Result);
            if (L1Result != 1)
            {
                ChangeStatus(L1ResultMsg, true);
                return false;
            }
            ChangeStatus(L1ResultMsg, false);
            int L1Lenght = answer[1];
            int L2Result = answer[2];
            string L2ResultMsg = ParseL2error(L2Result);
            if (L2Result != 0)
            {
                ChangeStatus(L2ResultMsg, true);
                return false;
            }
            //Проверка контрольной суммы
            byte cs = 0;
            for (int i = 0; i < answer.Length; i++)
            {
                cs ^= answer[i];
            }
            if (cs != 0)
            {
                ChangeStatus("Ошибка контрольной суммы", true);
                return false;
            }
            ChangeStatus(L2ResultMsg, false);
            return true;
        }

    }
}

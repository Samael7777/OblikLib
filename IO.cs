using System;
using System.IO.Ports;
using System.Threading;

namespace Oblik
{
    public partial class Oblik : IOblik
    {
        
        //Отправка запроса и получение данных Query - запрос, Answer - ответ
        private void OblikQuery(byte[] Query, out byte[] Answer)
        {
            object SerialIncoming = new object();         //Монитор таймаута чтения порта
            Answer = new byte[0];
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
                    ChangeStatus("Отправка запроса", false);
                    com.DataReceived += new SerialDataReceivedEventHandler(DataReciever);                   //событие чтения из порта
                    com.Write(Query, 0, Query.Length);                                                      //отправка буфера записи
                    com.DiscardInBuffer();                                                                  //очистка буфера приема
                    //Получение ответа
                    int r = _repeats;
                    bool ReadOk = false;
                    ChangeStatus("Ожидание ответа...", false);
                    while (r > 0)   //Повтор при ошибке
                    {
                        lock (SerialIncoming)
                        {
                            if (!Monitor.Wait(SerialIncoming, _timeout))
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
                        ChangeStatus("Нет данных", true);
                    }
                    com.Close();        //Закрыть порт
                    if (!_isError)
                    {
                        Array.Resize(ref Answer, _rbuf.Length);
                        Array.Copy(_rbuf, 0, Answer, 0, _rbuf.Length);
                    }
                }
                catch (Exception e)
                {
                    ChangeStatus(e.Message, true);
                }
                finally
                {
                    Query = null;
                }
            }
        }

        /*
       SegmentAccess
       Доступ к данным в сегменте счетчика, возвращает количество прочитанных байт ответа счетчика или -1 в случае ошибки
       data - данные для записи в сегмент, если запрос на чтение, то null
       len - количество байт для записи / чтения
       answ - полученные данные со счетчика (не обработанные)
       AccType: 0 - чтение, 1 - запись 
       */
        private bool SegmentAccsess(byte segment, UInt16 offset, byte len, byte[] data, byte AccType)
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
            byte[] answer;
            OblikQuery(_l1, out answer);

            //Заполняем структуру ответа счетчика
            if (!_isError)
            {
                AnswerParser(answer);
            }
            return _isError;
        }
        
        //Методы обработки фреймов L1, L2
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
            ChangeStatus(L1ResultMsg, false);
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
                    ChangeStatus(L2ResultMsg, true);
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
                }
                else
                {
                    ChangeStatus(L2ResultMsg, false);
                }
            }
        }
    }
}

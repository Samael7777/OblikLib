//Обеспечение операций ввода/вывода

using OblikControl.Resources;
using System;
using System.IO.Ports;
using System.Text;


namespace OblikControl
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
        /// Возвращает количество миллисекунд для данного экземпляра
        /// </summary>
        /// <returns>Количество миллисекунд для данного экземпляра</returns>
        private static ulong GetTickCount()
        {
            return (ulong)DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        /// <summary>
        /// Чтение данных из порта
        /// </summary>
        /// <param name="sp">Ссылка на порт</param>
        /// <param name="Timeout">Таймаут</param>
        /// <param name="BytesToRead">Количество байт для чтения</param>
        /// <param name="buffer">Буфер для считанных данных</param>
        private void ReadAnswer(SerialPort sp, int Timeout, int BytesToRead, out byte[] buffer)
        {
            int BytesGet;
            int count = BytesToRead;
            int offset = 0;
            buffer = new byte[BytesToRead];
            ulong start = GetTickCount();
            while (offset < BytesToRead)
            {
                if ((GetTickCount() - start) > (ulong)Timeout)
                {
                    ChangeIOStatus(StringsTable.Timeout);
                    throw new Exception(StringsTable.Timeout);
                }
                try
                {
                    BytesGet = (byte)sp.Read(buffer, offset, count);
                }
                catch
                {
                    BytesGet = 0;
                }
                count -= BytesGet;
                offset += BytesGet;
            }
            if (offset != BytesToRead) { throw new OblikIOException(StringsTable.ReadError); }
        }

        /// <summary>
        /// Отправка запроса к счетчику и получение данных
        /// </summary>
        /// <param name="Query">Запрос к счетчику в формате массива L1</param>
        /// <param name="Answer">Ответ счетчика в формате массива L1</param>
        /// <return>Успех</return>>
        public void OblikQuery(byte[] Query, out byte[] Answer)
        {
            bool success = false;           //Флаг успеха операции
            SerialPort com = null;
            try
            {
                com = new SerialPort
                {
                    PortName = "COM" + _ConParams.Port.ToString(),
                    BaudRate = _ConParams.Baudrate.Value,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    DtrEnable = false,
                    RtsEnable = false,
                    Handshake = Handshake.None
                };
                Answer = null;
                try
                {
                    com.Open();
                }
                catch (Exception e)
                {
                    ChangeIOStatus(e.Message);
                    throw new OblikIOException(e.Message);
                }
                int r = _ConParams.Repeats.Value;
                ChangeCmdStatus(StringsTable.SendReq);
                while ((r > 0) && (!success))   //Повтор при ошибке
                {
                    com.DiscardOutBuffer();                                                                 //очистка буфера передачи
                    com.DiscardInBuffer();                                                                  //очистка буфера приема
                    try
                    {
                        if (Query == null)
                        {
                            throw new ArgumentNullException(paramName: nameof(Query));
                        }
                        com.Write(Query, 0, Query.Length);
                    }
                    catch (Exception e)
                    {
                        ChangeIOStatus(e.Message);
                        throw new OblikIOException(e.Message);
                    }
                    try
                    {
                        Answer = new byte[2];
                        r--;
                        //Получение результата L1
                        ReadAnswer(com, _ConParams.Timeout.Value, 1, out byte[] ReadBuffer);
                        Answer[0] = ReadBuffer[0];
                        if (Answer[0] != 1) { throw new OblikIOException(ParseChannelError(Answer[0])); }
                        //Получение количества байт в ответе
                        ReadAnswer(com, _ConParams.Timeout.Value, 1, out ReadBuffer);
                        Answer[1] = ReadBuffer[0];
                        int len = ReadBuffer[0] + 1;
                        Array.Resize(ref Answer, len + 2);
                        //Получение всего ответа
                        ReadAnswer(com, (int)(_ConParams.Timeout.Value / 5u), len, out ReadBuffer);
                        ReadBuffer.CopyTo(Answer, 2);
                        success = (ReadBuffer.Length == len);
                        if (Answer[2] != 0) { throw new OblikIOException(ParseSegmentError(Answer[2])); }
                        //Проверка контрольной суммы
                        byte cs = 0;
                        for (int i = 0; i < Answer.Length; i++)
                        {
                            cs ^= Answer[i];
                        }
                        if (cs != 0)
                        {
                            throw new OblikIOException(StringsTable.CSCError);
                        }
                    }
                    catch (Exception e)
                    {
                        success = false;
                        ChangeIOStatus(e.Message);
                    }
                }
            }
            finally
            {
                if (com != null) { com.Dispose(); }
            }
            if (!success)
            {
                ChangeIOStatus(StringsTable.ReqError);
                throw new OblikIOException(StringsTable.QueryErr);
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
        private byte[] PerformFrame(byte Segment, UInt16 Offset, byte Len, Access Access, byte[] Data = null)
        {
            byte[] _l1;                                                     //Посылка 1 уровня
            byte[] _l2;                                                     //Посылка 2 уровня
            byte[] _pwdarray = new byte[8];                                 //Пароль к счетчику
            if (_ConParams.Password != null)
            {
                _pwdarray = Encoding.Default.GetBytes(_ConParams.Password);
            }
            //Формируем запрос L2
            _l2 = new byte[5 + (Len + 8) * (int)Access];                    //5 байт заголовка + 8 байт пароля + данные 
            _l2[0] = (byte)((Segment & 127) + (int)Access * 128);           //(биты 0 - 6 - номер сегмента, бит 7 = 1 - операция записи)
            _l2[1] = (byte)_ConParams.AccessLevel.Value;                    //Указываем уровень доступа
            _l2[2] = BitConverter.GetBytes(Offset)[1];                      //Старший байт смещения
            _l2[3] = BitConverter.GetBytes(Offset)[0];                      //Младший байт смещения
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
        public void SegmentRead(byte Segment, UInt16 Offset, byte Len, out byte[] Data)
        {
            byte[] Query = PerformFrame(Segment, Offset, Len, Access.Read);
            try
            {
                OblikQuery(Query, out byte[] answer);
                Data = ArrayPart(answer, 4, answer.Length - 5);
            }
            catch(OblikIOException)
            {
                string mes = StringsTable.SegReadErr + $" #{Segment}";
                ChangeSegStatus(mes);
                throw new OblikSegException(mes);
            }
        }

        /// <summary>
        /// Запись в сегмент счетчика
        /// </summary>
        /// <param name="Segment">Сегмент счетчика</param>
        /// <param name="Offset">Смещение относительно начала сегмента</param>
        /// <param name="Data">Данные для записи</param>
        /// <returns>Успех операции</returns>
        public void SegmentWrite(byte Segment, UInt16 Offset, byte[] Data)
        {
            if (Data == null)
            {
                throw new OblikIOException(StringsTable.DataError);
            }
            byte[] Query = PerformFrame(Segment, Offset, (byte)Data.Length, Access.Write, Data);
            try
            {
                OblikQuery(Query, out byte[] answer);
            }
            catch(OblikIOException) 
            {
                string mes = StringsTable.SegWriteErr + $" #{Segment}";
                ChangeSegStatus(mes);
                throw new OblikSegException(mes);
            }
        }

        /// <summary>
        /// Парсер ошибок L1
        /// </summary>
        /// <param name="error">Код ошибки L1</param>
        /// <returns>Строка с текстом ошибки</returns>
        private static string ParseChannelError(int error)
        {
            string res;
            switch (error)
            {
                case 1:
                    res = StringsTable.L1OK;
                    break;
                case 0xff:
                    res = StringsTable.L1CSCError;
                    break;
                case 0xfe:
                    res = StringsTable.L1Overflow;
                    break;
                default:
                    res = StringsTable.L1Unk;
                    break;
            }
            return res;
        }

        /// <summary>
        /// Парсер ошибок L2
        /// </summary>
        /// <param name="error">Код ошибки L2</param>
        /// <returns>Строка с текстом ошибки</returns>
        private static string ParseSegmentError(int error)
        {
            string res;
            switch (error)
            {
                case 0:
                    res = StringsTable.L2Err00;
                    break;
                case 0xff:
                    res = StringsTable.L2ErrFF;
                    break;
                case 0xfe:
                    res = StringsTable.L2ErrFE;
                    break;
                case 0xfd:
                    res = StringsTable.L2ErrFD;
                    break;
                case 0xfc:
                    res = StringsTable.L2ErrFC;
                    break;
                case 0xfb:
                    res = StringsTable.L2ErrFB;
                    break;
                case 0xfa:
                    res = StringsTable.L2ErrFA;
                    break;
                case 0xf9:
                    res = StringsTable.L2ErrF9;
                    break;
                case 0xf8:
                    res = StringsTable.L2ErrF8;
                    break;
                case 0xf7:
                    res = StringsTable.L2ErrF7;
                    break;
                case 0xf6:
                    res = StringsTable.L2ErrF6;
                    break;
                case 0xf5:
                    res = StringsTable.L2ErrF5;
                    break;
                default:
                    res = StringsTable.L2ErrUnk;
                    break;
            }
            return res;
        }

        /// <summary>
        /// Процедура шифрования данных L2
        /// </summary>
        /// <param name="l2">Ссылка на массив L2</param>
        /// <param name="passwd">Пароль</param>
        private static void Encode(ref byte[] l2, byte[] passwd)
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
    }
}

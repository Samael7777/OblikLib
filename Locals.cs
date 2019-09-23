//Локальные переменные

namespace Oblik
{

    //Класс счетчиков Облик
    public partial class Oblik : IOblik
    {
        //Локальные переменные
        /// <summary>
        /// Номер COM-порта счетчика
        /// </summary>
        private readonly int _port;
        /// <summary>
        /// Адрес счетчика в протоколе RS-485
        /// </summary>
        private readonly int _addr;
        /// <summary>
        /// Скорость работы порта, 9600 бод - по умолчанию
        /// </summary>
        private readonly int _baudrate;
        /// <summary>
        /// Таймаут, мс
        /// </summary>
        private int _timeout;
        /// <summary>
        /// Количество повторов при ошибке связи
        /// </summary>
        private int _repeats;
        /// <summary>
        /// Пароль к счетчику
        /// </summary>
        private byte[] _passwd;
        /// <summary>
        /// Пользователь
        /// 0 - пользователь;
        /// 1 - администратор;
        /// 2 - энергонадзор; 
        /// 3 - служебный пользователь.
        /// </summary>
        private byte _user;
        /// <summary>
        /// Параметры вычислений
        /// </summary>
        private CalcUnitsStruct _CalcUnits;
        /// <summary>
        /// Фрейм L1 ответа счетчика
        /// </summary>
        int L1Result;
        /// <summary>
        /// Фрейм L1 ответа счетчика, расшифровка
        /// </summary>
        string L1ResultMsg;
        /// <summary>
        /// Количетво байт в полях "Длина", "L2Data", "Результат"
        /// </summary>
        int L1Lenght;
        /// <summary>
        /// Контрольная сумма фрейма L1
        /// </summary>
        int L1Sum;        
        /// <summary>
        /// Фрейм L2 ответа счетчика
        /// </summary>
        int L2Result;
        /// <summary>
        /// Фрейм L2 ответа счетчика, расшифровка
        /// </summary>
        string L2ResultMsg;
        /// <summary>
        /// Количество данных, успешно обработанных операцией
        /// </summary>
        int L2Lenght;
        /// <summary>
        /// Данные L2
        /// </summary>
        byte[] L2Data;
        /// <summary>
        /// Тип доступа к сегменту
        /// 1 - на запись,
        /// 0 - на чтение
        /// </summary>
        enum Access : byte
        {
            Write = 1,
            Read = 0
        }
    }
}
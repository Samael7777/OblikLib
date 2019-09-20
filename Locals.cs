using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;



namespace Oblik
{

    //Класс счетчиков Облик
    public partial class Oblik : IOblik
    {
        //Локальные переменные
        private readonly int _port;                     //порт счетчика
        private readonly int _addr;                     //адрес счетчика
        private readonly int _baudrate;                 //скорость работы порта, 9600 бод - по умолчанию
        private int _timeout, _repeats;                 //таймаут и повторы
        private byte[] _passwd;                         //пароль
        private bool _isError;                          //Наличие ошибки
        private byte _user;                             //Пользователь от 0 до 3 (3 - максимальные привелегии, 0 - минимальные)

        private CalcUnitsStruct _CalcUnits;             //Параметры вычислений

        int L1Result;                                   //Результат фрейма L1
        string L1ResultMsg;                             //Результат фрейма L1, расшифровка
        int L1Lenght;                                   //Количетво байт в полях "Длина", "L2Data", "Результат" 
        int L1Sum;                                      //Контрольная сумма
        int L2Result;                                   //Результат запроса L2
        string L2ResultMsg;                             //Результат запроса L2, расшифровка
        int L2Lenght;                                   //Количество данных, успешно обработанных операцией
        byte[] L2Data;                                  //Данные L2
    }
}
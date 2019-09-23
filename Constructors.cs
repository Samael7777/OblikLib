//Конструкторы класса

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oblik
{
    public partial class Oblik : IOblik
    {
        //Конструкторы

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="port">Порт</param>
        /// <param name="baudrate">Скорость соединения</param>
        /// <param name="addr">Адрес счетчика в протоколе RS-485</param>
        /// <param name="timeout">Время ожидания ответа</param>
        /// <param name="repeats">Количество повторов при опросе</param>
        /// <param name="password">Пароль</param>
        public Oblik(int port, int baudrate, int addr, int timeout, int repeats, string password)
        {
            _port = port;
            _addr = addr;
            _timeout = timeout;
            _repeats = repeats;
            _baudrate = baudrate;
            _passwd = new byte[8];
            if (password == "")
            {
                for (int i = 0; i < 8; i++) { _passwd[i] = 0; }
            }
            else
            {
                _passwd = Encoding.Default.GetBytes(password);
            }
            _user = 2;
            _CalcUnits = new CalcUnitsStruct();
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="port">Порт</param>
        /// <param name="addr">Адрес счетчика в протоколе RS-485</param>
        /// <param name="timeout">Время ожидания ответа</param>
        /// <param name="repeats">Количество повторов</param>
        public Oblik(int port, int addr, int timeout, int repeats) : this(port, 9600, addr, timeout, repeats, "") { }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="port">Порт</param>
        /// <param name="addr">Адрес счетчика в протоколе RS-485</param>
        public Oblik(int port, int addr) : this(port, 9600, addr, 500, 2, "") { }
    }
}

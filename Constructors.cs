//Конструкторы класса

namespace Oblik
{
    public partial class Oblik
    {

        /// <summary>
        /// Параметры соединения
        /// </summary>
        OblikConnection _ConParams;

        /// <summary>
        /// Параметры вычислений
        /// </summary>
        private CalcUnitsStruct _CalcUnits;

        //Конструкторы
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="Connection">Параметры подключения</param>
        public Oblik(OblikConnection Connection)
        {
            _CalcUnits = new CalcUnitsStruct();
            _ConParams.AccessLevel = (Connection.AccessLevel != null) ? Connection.AccessLevel : AccessLevel.Energo;
            _ConParams.Address = (Connection.Address != null) ? Connection.Address : 0x01;
            _ConParams.Baudrate = (Connection.Baudrate != null) ? Connection.Baudrate : 9600;
            _ConParams.Password = Connection.Password;
            _ConParams.Port = (Connection.Port != null) ? Connection.Port : 1;
            _ConParams.Repeats = (Connection.Repeats != null) ? Connection.Repeats : 5;
            _ConParams.Timeout = (Connection.Timeout != null) ? Connection.Timeout : 2000;
        }

        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="Port">Номер COM порта</param>
        /// <param name="Address">Адрес счетчика в сети RS-485</param>
        public Oblik(int Port, int Address) : this (
            new OblikConnection
            {
            AccessLevel = AccessLevel.Energo,
            Address = Address,
            Baudrate = 9600,
            Password = "",
            Port = Port,
            Repeats = 5,
            Timeout = 2000
            })   { }
    }
}

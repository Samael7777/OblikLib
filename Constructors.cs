﻿//Конструкторы класса
using System.Resources;
using OblikControl.Resources;

[assembly: NeutralResourcesLanguageAttribute("ru")]

namespace OblikControl
{
    /// <summary>
    /// Класс для работы со счетчиками Облик
    /// </summary>
    public partial class Oblik
    {
        /// <summary>
        /// Параметры соединения
        /// </summary>
        OblikConnection _ConParams;

        /// <summary>
        /// Параметры вычислений
        /// </summary>
        CalcUnitsStruct _CalcUnits;

        //Конструкторы
        /// <summary>
        /// Конструктор класса
        /// </summary>
        /// <param name="Connection">Параметры подключения</param>
        public Oblik(OblikConnection Connection)
        {
            //Заглушкии для событий
            Dummy dummy = new Dummy();
            OnProgress += dummy.DummyEventHandler;
            OnCmdStatusChange += dummy.DummyEventHandler;
            OnSegStatusChange += dummy.DummyEventHandler;
            OnIOStatusChange += dummy.DummyEventHandler;

            _CalcUnits = new CalcUnitsStruct();
            _ConParams.AccessLevel = (Connection.AccessLevel != null) ? Connection.AccessLevel : AccessLevel.Energo;
            _ConParams.Address = (Connection.Address != null) ? Connection.Address : 0x01;
            _ConParams.Baudrate = (Connection.Baudrate != null) ? Connection.Baudrate : 9600;
            _ConParams.Password = Connection.Password;
            _ConParams.Port = (Connection.Port != null) ? Connection.Port : 1;
            _ConParams.Repeats = (Connection.Repeats != null) ? Connection.Repeats : 2;
            _ConParams.Timeout = (Connection.Timeout != null) ? Connection.Timeout : 1000;
        }

        /// <summary>
        /// Конструктор класса (соединение на скорости 9600 бод, пользователь Энергонадзор, без пароля, таймаут соединения 1000 мс, 2 повтора)
        /// </summary>
        /// <param name="Port">Номер COM порта</param>
        /// <param name="Address">Адрес счетчика в сети RS-485</param>
        public Oblik(int Port, int Address) : this(
            new OblikConnection
            {
                AccessLevel = AccessLevel.Energo,
                Address = Address,
                Baudrate = 9600,
                Password = "",
                Port = Port,
                Repeats = 2,
                Timeout = 1000
            })
        { }

        /// <summary>
        /// Возвращает тип счетчика и версию его ПО
        /// </summary>
        /// <returns>Тип счетчика и версия его ПО</returns>
        public override string ToString()
        {
            string text = string.Empty;
            FirmwareVer fw = default;
            int v1, v2;
            try
            {
                GetFWVersion(out fw);
            }
            catch
            {
                ChangeCmdStatus(StringsTable.Error);
            }
            finally
            {
                v1 = fw.Version & 15;
                v2 = fw.Version & 240;
                switch (v2)
                {
                    case 0:
                        text = StringsTable.CntrType1;
                        break;
                    case 1:
                        text = StringsTable.CntrType2;
                        break;
                    case 2:
                        text = StringsTable.CntrType3;
                        break;
                    case 3:
                        text = StringsTable.CntrType4;
                        break;
                }
                text += $" V.{v1}.{fw.Build} mod.{v2} ";
            }
            return text;
        }
    }
}

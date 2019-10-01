//Конструкторы класса
using OblikControl.Resources;
using System.Resources;

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
            OnStatusChange += dummy.DummyEventHandler;

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
        public Oblik(int Port, int Address) : this(
            new OblikConnection
            {
                AccessLevel = AccessLevel.Energo,
                Address = Address,
                Baudrate = 9600,
                Password = "",
                Port = Port,
                Repeats = 5,
                Timeout = 2000
            })
        { }

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
                ChangeStatus(StringsTable.Error);
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

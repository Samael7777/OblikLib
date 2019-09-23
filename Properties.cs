//Публичные свойства класса

using System.Text;

namespace Oblik
{
    public partial class Oblik : IOblik
    {
        //Общие свойства

        /// <summary>
        /// Количество провторов при опросе
        /// </summary>
        public int Repeats
        {
            set => _repeats = value;
            get => _repeats;
        }

        /// <summary>
        /// Время ожидания ответа
        /// </summary>
        public int Timeout
        {
            set => _timeout = value;
            get => _timeout;
        }

        /// <summary>
        /// Пароль к счетчику
        /// </summary>
        public string Password
        {
            set => _passwd = Encoding.Default.GetBytes(value);
            get => Encoding.Default.GetString(_passwd);
        }

        /// <summary>
        /// Пользователь
        /// 0 - пользователь;
        /// 1 - администратор;
        /// 2 - энергонадзор; 
        /// 3 - служебный пользователь.
        /// </summary>
        public int User
        {
            set
            {
                if (value > 3) { _user = 3; }
                if (value < 0) { _user = 0; }
                _user = (byte)value;
            }
            get => _user;
        }

        /// <summary>
        /// Параметры вычислений
        /// </summary>
        public CalcUnitsStruct CalcUnits
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
    }
}

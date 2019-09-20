using System.Text;

namespace Oblik
{
    public partial class Oblik : IOblik
    {
        //Общие свойства
        public int Repeats
        {
            set => _repeats = value;
            get => _repeats;
        }
        public int Timeout
        {
            set => _timeout = value;
            get => _timeout;
        }
        public bool IsError
        {
            set => _isError = value;
            get => _isError;
        }
        public string Password
        {
            set => _passwd = Encoding.Default.GetBytes(value);
            get => Encoding.Default.GetString(_passwd);
        }
        public int User
        {
            set => _user = (byte)value;
            get => _user;
        }
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

//Публичные свойства класса

namespace Oblik
{
    public partial class Oblik
    {
        //Общие свойства
        /// <summary>
        /// Параметры соединения
        /// </summary>
        public OblikConnection ConnectionParams
        {
            get => _ConParams;
            set => _ConParams = value;
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

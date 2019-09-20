using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oblik
{
    public interface IOblik
    {
        
        //Свойства
        int Repeats { get; set; }                           //Количество повторов передачи
        int Timeout { get; set; }                           //Таймаут соединения
        bool IsError { get; set; }                          //Индикатор наличия ошибки           
        string Password { get; set; }                       //Пароль счетчика                      
        int User { get; set; }                              //Пользователь                           
        CalcUnitsStruct CalcUnits { get; set; }             //Параметры вычислений
       
        
        //Методы

        int GetDayGraphRecs();                                //Получить количество записей суточного графика
        bool CleanDayGraph();                                 //Стирание суточного графика
        bool SetCurrentTime();                                //Установка текущего времени в счетчике
        List<DayGraphRow> GetDayGraph(uint lines, uint offset);   //Получение суточного графика: lines - количество строк, offset - смещение (в строках)
        bool GetFWVersion(out FirmwareVer fw);                //Получение версии ПО счетчика
        bool GetCurrentValues(out CurrentValues values);      //Получение текущих значений
    }
}

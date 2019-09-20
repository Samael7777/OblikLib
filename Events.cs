using System;



namespace Oblik
{
    //Классы событий
    public class ProgressEventArgs : EventArgs { public float progress; };     //Аргументы события прогресса данных
    public class StatusChangeArgs : EventArgs                                                           
    {
        public string Message;
        public bool Error;
    };                                          //Аргументы события изменения статуса

    public partial class Oblik : IOblik
    {
        //Делегаты событий класса
        public delegate void Progress(object sender, ProgressEventArgs e);
        public delegate void StatusChange(object sender, StatusChangeArgs e);

        //События класса
        public event Progress OnProgress;               //Событие прогресса данных
        public event StatusChange OnStatusChange;       //Событие изменения статуса

        //Генераторы событий
        private void ChangeStatus(string message, bool error)      //Вызов события изменения статуса
        {
            StatusChangeArgs args = new StatusChangeArgs();
            args.Message = message;
            args.Error = error;
            _isError = error;
            if (OnStatusChange != null)
            {
                OnStatusChange(this, args);
            }
        }
        private void SetProgress(float progress)                   //Вызов события прогресса
        {
            ProgressEventArgs args = new ProgressEventArgs();
            args.progress = progress;
            if (OnProgress != null)
            {
                OnProgress(this, args);
            }

        }
    }
}
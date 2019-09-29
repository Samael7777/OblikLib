//События класса

using System;

namespace OblikControl
{
    //Классы событий

    /// <summary>
    /// Класс аргументов событий прогресса данных
    /// </summary>
    public class ProgressEventArgs : EventArgs
    {
        private float progress;
        /// <summary>
        /// Прогресс в процентах
        /// </summary>
        public float Progress { get => progress; set => progress = value; }
    }

    /// <summary>
    /// Класс аргументов событий изменения статуса
    /// </summary>
    public class StatusChangeEventArgs : EventArgs
    {
        private string message;
        private bool error;
        
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get => message; set => message = value; }
        /// <summary>
        /// Флаг ошибки
        /// </summary>
        public bool Error { get => error; set => error = value; }
    }

    public partial class Oblik
    {

        //Делегаты событий класса
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        public delegate void StatusChangeEventHandler(object sender, StatusChangeEventArgs e);

        /// <summary>
        /// Событие прогресса данных
        /// </summary>
        public event ProgressEventHandler OnProgress;

        /// <summary>
        /// Событие изменения статуса
        /// </summary>
        public event StatusChangeEventHandler OnStatusChange;

        //Генераторы событий

        /// <summary>
        /// Вызов события изменения статуса
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="error">Флаг ошибки</param>
        private void ChangeStatus(string message, bool error)
        {
            StatusChangeEventArgs args = new StatusChangeEventArgs
            {
                Message = message,
                Error = error
            };
            OnStatusChange.Invoke(this, args);
        }

        /// <summary>
        /// Вызов события прогресса
        /// </summary>
        /// <param name="progress">Прогресс в процентах</param>
        private void SetProgress(float progress)
        {
            ProgressEventArgs args = new ProgressEventArgs
            {
                Progress = progress
            };
            OnProgress.Invoke(this, args);
        }       
    }

    //Класс - заглушка для событий
    internal class Dummy
    {
        internal void DummyEventHandler(object sender, EventArgs e) { }
    }
}
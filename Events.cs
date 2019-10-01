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
        /// <summary>
        /// Прогресс в процентах
        /// </summary>
        public float Progress { get; set; }
    }

    /// <summary>
    /// Класс аргументов событий изменения статуса
    /// </summary>
    public class StatusChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; set; }
    }

    public partial class Oblik
    {
        /// <summary>
        /// Делегат события прогресса операции
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ProgressEventHandler(object sender, ProgressEventArgs e);
        /// <summary>
        /// Делегат события изменения статуса
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// Вызов события изменения статуса. В случае установки флага ошибки, вызывается исключение OblikException
        /// </summary>
        /// <param name="message">Сообщение</param>
        private void ChangeStatus(string message)
        {
            StatusChangeEventArgs args = new StatusChangeEventArgs
            {
                Message = message
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
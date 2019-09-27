//События класса

using System;

namespace Oblik
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
        public float progress;
    }

    /// <summary>
    /// Класс аргументов событий изменения статуса
    /// </summary>
    public class StatusChangeArgs : EventArgs
    {
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message;
        /// <summary>
        /// Флаг ошибки
        /// </summary>
        public bool Error;
    }

    public partial class Oblik
    {
        //Делегаты событий класса
        public delegate void Progress(object sender, ProgressEventArgs e);
        public delegate void StatusChange(object sender, StatusChangeArgs e);

        /// <summary>
        /// Событие прогресса данных
        /// </summary>
        public event Progress OnProgress;

        /// <summary>
        /// Событие изменения статуса
        /// </summary>
        public event StatusChange OnStatusChange;

        //Генераторы событий

        /// <summary>
        /// Вызов события изменения статуса
        /// </summary>
        /// <param name="message">Сообщение</param>
        /// <param name="error">Флаг ошибки</param>
        private void ChangeStatus(string message, bool error)
        {
            StatusChangeArgs args = new StatusChangeArgs
            {
                Message = message,
                Error = error
            };
            OnStatusChange?.Invoke(this, args);
        }

        /// <summary>
        /// Вызов события прогресса
        /// </summary>
        /// <param name="progress">Прогресс в процентах</param>
        private void SetProgress(float progress)
        {
            ProgressEventArgs args = new ProgressEventArgs
            {
                progress = progress
            };
            OnProgress?.Invoke(this, args);

        }
    }
}
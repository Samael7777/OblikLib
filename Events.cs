//События класса

using System;

namespace OblikControl
{
    //Классы событий

    /// <summary>
    /// Общий класс событий Облик
    /// </summary>
    public class OblikEventArgs : EventArgs
    {
        /// <summary>
        /// Сообщение
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Класс аргументов событий прогресса данных
    /// </summary>
    public class OblikProgressEventArgs : OblikEventArgs
    {
        /// <summary>
        /// Прогресс в процентах
        /// </summary>
        public float Progress { get; set; }
    }

    /// <summary>
    /// Класс аргументов событий изменения статуса команд
    /// </summary>
    public class OblikCmdStatusChangeEventArgs : OblikEventArgs { }
    /// <summary>
    /// Класс аргументов событий изменения статуса операций с сегментами
    /// </summary>
    public class OblikSegStatusChangeEventArgs : OblikEventArgs { }
    /// <summary>
    /// Класс аргументов событий изменения статуса операций ввода/вывода
    /// </summary>
    public class OblikIOStatusChangeEventArgs : OblikEventArgs { }

    public partial class Oblik
    {
        /// <summary>
        /// Делегат события прогресса операции
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void ProgressEventHandler(object sender, OblikProgressEventArgs e);
        /// <summary>
        /// Делегат события изменения статуса команд
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void CmdStatusChangeEventHandler(object sender, OblikCmdStatusChangeEventArgs e);
        /// <summary>
        /// Делегат события изменения статуса операций с сегментами
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void SegStatusChangeEventHandler(object sender, OblikSegStatusChangeEventArgs e);
        /// <summary>
        /// Делегат события изменения статуса операций ввода/вывода
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void IOStatusChangeEventHandler(object sender, OblikIOStatusChangeEventArgs e);
        
        /// <summary>
        /// Событие прогресса данных
        /// </summary>
        public event ProgressEventHandler OnProgress;
        /// <summary>
        /// Событие изменения статуса
        /// </summary>
        public event CmdStatusChangeEventHandler OnCmdStatusChange;
        /// <summary>
        /// Событие изменения статуса операций с сегментами
        /// </summary>
        public event SegStatusChangeEventHandler OnSegStatusChange;
        /// <summary>
        /// Событие изменения статуса операций ввода/вывода
        /// </summary>
        public event IOStatusChangeEventHandler OnIOStatusChange;

        //Генераторы событий

        /// <summary>
        /// Вызов события изменения статуса команды.
        /// </summary>
        /// <param name="message">Сообщение</param>
        private void ChangeCmdStatus(string message)
        {
            OblikCmdStatusChangeEventArgs args = new OblikCmdStatusChangeEventArgs
            {
                Message = message
            };
            OnCmdStatusChange.Invoke(this, args);
        }
        /// <summary>
        /// Вызов события изменения статуса операций с сегментами
        /// </summary>
        /// <param name="message">Сообщение</param>
        private void ChangeSegStatus(string message)
        {
            OblikSegStatusChangeEventArgs args = new OblikSegStatusChangeEventArgs
            {
                Message = message
            };
            OnSegStatusChange.Invoke(this, args);
        }
        /// <summary>
        /// Вызов события изменения статуса операций ввода/вывода
        /// </summary>
        /// <param name="message">Сообщение</param>
        private void ChangeIOStatus(string message)
        {
            OblikIOStatusChangeEventArgs args = new OblikIOStatusChangeEventArgs
            {
                Message = message
            };
            OnIOStatusChange.Invoke(this, args);
        }

        /// <summary>
        /// Вызов события прогресса
        /// </summary>
        /// <param name="progress">Прогресс в процентах</param>
        private void SetProgress(float progress)
        {
            OblikProgressEventArgs args = new OblikProgressEventArgs
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Game2.Engine;

namespace CardsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Глобальные переменные
        // листы
        private List<Card> _tableCards;
        private List<Card> _firstPlayerCards;
        private List<Card> _secondPlayerCards;
        // карты
        private object _firstCard;
        private object _secondCard;
        // переменные
        private bool _isOver;
        private bool _isStepMode;
        private bool _isSkip;
        private int _speed = 800;
        private int _steps = 0;
        // дополнительные элементы
        private BackgroundWorker _backgroundWorker;
        private TaskCompletionSource<bool> _TCS = new();
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            InitBackgroundWorker();
        }

        #region BackgroundWorker

        /// <summary>
        /// Инициализация BackgroundWorker'а
        /// </summary>
        private void InitBackgroundWorker()
        {
            _backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            _backgroundWorker.DoWork += BackgroundWorker_DoWork;
            _backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            _backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Событие завершения BackgroundWorker'а
        /// </summary>
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Result();
        }

        /// <summary>
        /// Событие изменения прогресса BackgroundWorker'а
        /// </summary>
        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateData();
        }

        /// <summary>
        /// Событие запуска операции BackgroundWorker'а
        /// </summary>
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            GameStart(e);
        }

        #endregion

        #region Обновление и вывод данных

        /// <summary>
        /// Обновление данных
        /// </summary>
        private void UpdateData()
        {
            // если ход завершён, скрывает карты
            if (_isOver)
                GridTable.Visibility = Visibility.Hidden;
            else
                GridTable.Visibility = Visibility.Visible;

            TbkFirstPlayerDeck.Text = _firstPlayerCards.Count.ToString();
            TbkSecondPlayerDeck.Text = _secondPlayerCards.Count.ToString();
            TbkSteps.Text = $"Ход: {_steps}";
            CardFirstPlayer.DataContext = _firstCard;
            CardSecondPlayer.DataContext = _secondCard;
        }

        /// <summary>
        /// Метод переопределения данных
        /// </summary>
        private void RefreshData()
        {
            _steps = 0;
            _firstCard = _secondCard = new Card();
            _firstPlayerCards = new List<Card>();
            _secondPlayerCards = new List<Card>();
            _tableCards = new List<Card>();
            CardFirstPlayer.DataContext = CardSecondPlayer.DataContext = null;
        }

        /// <summary>
        /// Метод с подсчётом и выводом результатов игры
        /// </summary>
        private void Result()
        {
            // определение победителя
            string winner;
            if (_firstPlayerCards.Count > _secondPlayerCards.Count)
                winner = "Победил первый игрок!";
            else if (_firstPlayerCards.Count < _secondPlayerCards.Count)
                winner = "Победил второй игрок!";
            else
                winner = "Ничья!\nИ такое бывает :)";

            // обновление данных и вывод сообщения
            UpdateData();
            MessageBox.Show(winner, "Конец игры", MessageBoxButton.OK, MessageBoxImage.Information);
            RefreshData();
            UpdateData();

            GridTable.Visibility = Visibility.Hidden;
            BtnStepMode.IsEnabled = BtnAutoMode.IsEnabled = SPSpeed.IsEnabled = true;
            _isStepMode = _isSkip = BtnStop.IsEnabled = false;
            BtnStepMode.Content = "Пошаговая";
        }

        #endregion

        #region Алгоритм игры

        /// <summary>
        /// Метод запуска игры
        /// </summary>
        /// <param name="e">null - пошаговый режим, DoWarkEventArgs - автоматический режим</param>
        private async void GameStart(DoWorkEventArgs e)
        {
            // перетасовка основной колоды между игроками
            List<Card> deck = new Deck(new Random()).ToList();
            Shuffle(_firstPlayerCards, _secondPlayerCards, deck);

            while (_firstPlayerCards.Count != 0 && _secondPlayerCards.Count != 0)
            {
                // обнуление параметров
                _steps++;
                _isOver = false;
                int index = 0;
                _tableCards.Clear();

                // выкладование первой парты карт на стол
                CardsOnTable();
                await GamePause(e);

                // проверка первой пары карт
                if (IsCardWin(_tableCards[index], _tableCards[index + 1]))
                {
                    _isOver = true;
                    await GamePause(e);
                    continue;
                }

                // если первая пара карт равна, то начало войны
                while (_firstPlayerCards.Count != 0 && _secondPlayerCards.Count != 0)
                {
                    CardsOnTable();
                    index += 2;
                    if (index % 4 == 0)
                    {
                        await GamePause(e);
                        if (IsCardWin(_tableCards[index], _tableCards[index + 1]))
                        {
                            _isOver = true;
                            await GamePause(e);
                            break;
                        }
                    }
                    else
                    {
                        _firstCard = _secondCard = null;
                        await GamePause(e);
                    }
                }
            }
            if (_isStepMode)
                Result();
        }

        /// <summary>
        /// Метод остановки игры
        /// </summary>
        /// <param name="e">null - пошаговый режим, DoWarkEventArgs - автоматический режим</param>
        private async Task GamePause(DoWorkEventArgs e)
        {
            if (e != null)
            {
                // завершение BackgroundWorker'а в случае вызова отмены
                if (_backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                Thread.Sleep(_speed);
                _backgroundWorker.ReportProgress(0);
            }
            else if (!_isSkip)
            {
                // ожидание нажатия кнопки продолжить
                UpdateData();
                await _TCS.Task;
                UpdateData();
            }
        }

        #region Работа с картами

        /// <summary>
        /// Метод выкладывания карт на стол
        /// </summary>
        private void CardsOnTable()
        {
            // активные карты
            _firstCard = _firstPlayerCards.LastOrDefault();
            _secondCard = _secondPlayerCards.LastOrDefault();
            // удаление карт из листов игроков
            _firstPlayerCards.Remove((Card)_firstCard);
            _secondPlayerCards.Remove((Card)_secondCard);
            // добавление активных карт на стол
            _tableCards.Add((Card)_firstCard);
            _tableCards.Add((Card)_secondCard);
        }

        /// <summary>
        /// Метод проверки карт между собой
        /// </summary>
        /// <param name="firstCard">первая карта</param>
        /// <param name="secondCard">вторая карта</param>
        /// <returns>True - победа одного из игроков,
        /// False - война</returns>
        private bool IsCardWin(Card firstCard, Card secondCard)
        {
            // буферные листы карт на столе каждого из игроков отдельно
            List<Card> firstPlayerBuff = new();
            List<Card> secondPlayerBuff = new();
            Shuffle(firstPlayerBuff, secondPlayerBuff, _tableCards);

            // минимальное количество элементов в листах
            int count = 0;
            if (firstPlayerBuff.Count >= secondPlayerBuff.Count)
                count = firstPlayerBuff.Count;
            if (secondPlayerBuff.Count > firstPlayerBuff.Count)
                count = secondPlayerBuff.Count;

            // возврат карт в руку игрока
            for (int i = 0; i < count; i++)
            {
                if (firstCard.Rank.Value > secondCard.Rank.Value)
                    ReceivingCards(firstPlayerBuff, secondPlayerBuff, _firstPlayerCards);
                else if (firstCard.Rank.Value < secondCard.Rank.Value)
                    ReceivingCards(secondPlayerBuff, firstPlayerBuff, _secondPlayerCards);
            }

            // завершение проверки в случае опустошения одного из листов
            if (firstPlayerBuff.Count == 0 || secondPlayerBuff.Count == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Метод возврата карт в руку к игроку
        /// </summary>
        /// <param name="firstPlayerBuff">буферный лист первого игрока</param>
        /// <param name="secondPlayerBuff">буферный лист второго игрока</param>
        /// <param name="PlayerCards">основной лист игрока</param>
        private static void ReceivingCards(List<Card> firstPlayerBuff, List<Card> secondPlayerBuff, List<Card> PlayerCards)
        {
            if (firstPlayerBuff.Count != 0)
            {
                PlayerCards.Insert(0, firstPlayerBuff.LastOrDefault());
                firstPlayerBuff.Remove(firstPlayerBuff.LastOrDefault());
            }
            if (secondPlayerBuff.Count != 0)
            {
                PlayerCards.Insert(0, secondPlayerBuff.LastOrDefault());
                secondPlayerBuff.Remove(secondPlayerBuff.LastOrDefault());
            }
        }

        /// <summary>
        /// Перетасовка карт
        /// </summary>
        /// <param name="firstList">Лист карт первого игрока</param>
        /// <param name="secondList">Лист карт второго игрока</param>
        /// <param name="mainList">Основной лист</param>
        private static void Shuffle(List<Card> firstList, List<Card> secondList, List<Card> mainList)
        {
            int i = 1;
            foreach (var item in mainList)
            {
                switch (i)
                {
                    case 1:
                        firstList.Add(item);
                        i = 2;
                        break;
                    case 2:
                        secondList.Add(item);
                        i = 1;
                        break;
                }
            }
        }

        #endregion 

        #region Скорость игры

        private void RbSlow_Click(object sender, RoutedEventArgs e)
        {
            _speed = 1500;
        }

        private void RbMiddle_Click(object sender, RoutedEventArgs e)
        {
            _speed = 800;
        }

        private void RbFast_Click(object sender, RoutedEventArgs e)
        {
            _speed = 450;
        }

        private void RbMegaFast_Click(object sender, RoutedEventArgs e)
        {
            _speed = 1;
        }

        #endregion

        #endregion

        #region Кнопки

        /// <summary>
        /// Событие кнопки, включающее пошаговый режим
        /// </summary>
        private void BtnStepMode_Click(object sender, RoutedEventArgs e)
        {
            // если пошаговый режим выключен то включить
            if (!_backgroundWorker.IsBusy && _isStepMode == false)
            {
                RefreshData();
                BtnAutoMode.IsEnabled = SPSpeed.IsEnabled = false;
                BtnStop.IsEnabled = true;
                BtnStepMode.Content = "Продолжить";
                _isStepMode = true;
                GameStart(null);
                return;
            }

            // продолжение кода после паузы
            _TCS.SetResult(true);
            _TCS = new TaskCompletionSource<bool>();
        }

        /// <summary>
        /// Событие кнопки, включающее автоматический режим
        /// </summary>
        private void BtnAutoMode_Click(object sender, RoutedEventArgs e)
        {
            // запуск автоматического режима
            if (!_backgroundWorker.IsBusy)
            {
                RefreshData();
                _backgroundWorker.RunWorkerAsync();
                BtnAutoMode.IsEnabled = BtnStepMode.IsEnabled = false;
                BtnStop.IsEnabled = true;
            }
        }

        /// <summary>
        /// Событие кнопки, останавливающее автоматический режим
        /// </summary>
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_backgroundWorker.IsBusy)
            {
                // завершение BackgroundWorker'а
                _backgroundWorker.CancelAsync();
            }
            else if (_isStepMode)
            {
                // завершение пошагового режима
                _isSkip = true;
                _TCS.SetResult(true);
                _TCS = new TaskCompletionSource<bool>();
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;

namespace Q_LearningVisualization {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            qLabels = QTable.Children.Cast<UIElement>().Where(x => x is Label l && l.Tag != null && Regex.IsMatch((string)l.Tag, @"\d \d")).Select(x => x as Label).ToList();
            qArrows = grid.Children.Cast<UIElement>().Where(x => x is Border b && b.Child is Canvas).Select(x => (Canvas)(x as Border).Child).ToList();
        }

        private void ClearButton_Click (object sender, RoutedEventArgs e) {
            Log.Text = "";
        }

        private bool AutoScroll = true;

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            var scroll = (ScrollViewer)sender;
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentHeightChange == 0) {   // Content unchanged : user scroll event
                if (scroll.VerticalOffset == scroll.ScrollableHeight) {   // Scroll bar is in bottom
                                                                                      // Set auto-scroll mode
                    AutoScroll = true;
                }
                else {   // Scroll bar isn't in bottom
                         // Unset auto-scroll mode
                    AutoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (AutoScroll && e.ExtentHeightChange != 0) {   // Content changed and auto-scroll mode set
                                                             // Autoscroll
                scroll.ScrollToVerticalOffset(scroll.ExtentHeight);
            }
        }

        readonly List<Label> qLabels;
        readonly List<Canvas> qArrows;
        QLearningModel qrunner;

        private DispatcherTimer timer = new DispatcherTimer();
        private void Button_Click(object sender, RoutedEventArgs e) {
            qLabels.ForEach(x => {
                x.Content = "0";
                x.Background = new SolidColorBrush(Colors.Yellow);
            });
            qArrows.ForEach(x => {
                x.Children.Cast<Grid>().ToList().ForEach(g => {
                    ((Polygon)g.Children[0]).Fill = new SolidColorBrush(Colors.Black);
                    ((Label)g.Children[1]).Content = "0.000";
                });
            });
            int delay = int.Parse(Delay.Text);
            
            PauseContinue.IsEnabled = delay > 0;

            RunButton.IsEnabled = false;
            DiscountFactor.IsEnabled = false;
            LearningRate.IsEnabled = false;
            Iterations.IsEnabled = false;
            LivingPenalty.IsEnabled = false;
            Delay.IsEnabled = false;

            ShowHidePolicy.IsEnabled = false;

            var iterations = int.Parse(Iterations.Text);
            int current = 1;
            qrunner = new QLearningModel(double.Parse(DiscountFactor.Text), double.Parse(LearningRate.Text), double.Parse(LivingPenalty.Text));
            Log.Text += $"Run started with γ={DiscountFactor.Text} and α={LearningRate.Text} and {Iterations.Text} iterations.\n";

            //No delay, calculate immediately
            if (delay < 1) {
                for (; current <= iterations; current++) {
                    while (!qrunner.GameOver()) {
                        qrunner.Iterate();
                    }
                    qrunner.Reset();
                }

                qLabels.ForEach(x => {
                    var tag = ((string)x.Tag).Split();
                    var state = int.Parse(tag[0]);
                    var action = int.Parse(tag[1]);

                    x.Content = qrunner.qtable[state][action];
                    x.Background = new SolidColorBrush(GenerateRedYellowGreenGradient(qrunner.qtable[state][action]));
                });

                qArrows.ForEach(x => {
                    var state = ((string)x.Tag)[0] - '0';
                    for (int i = 0; i < 4; i++) {
                        var grid = x.Children[i] as Grid;
                        var poly = grid.Children[0] as Polygon;
                        var label = grid.Children[1] as Label;
                        var q = qrunner.qtable[state][i];
                        poly.Fill = new SolidColorBrush(GenerateRedBlackGreenGradient(q));
                        label.Content = $"{q:0.000}";
                    }
                });
                Log.Text += iterations;
                RunButton.IsEnabled = true;
                DiscountFactor.IsEnabled = true;
                LearningRate.IsEnabled = true;
                Iterations.IsEnabled = true;
                LivingPenalty.IsEnabled = true;
                Delay.IsEnabled = true;
                ShowHidePolicy.IsEnabled = true;

                Log.Text += "\nRun ended.\n";
                return;
            }


            bool first = true;
            timer = new DispatcherTimer();

            timer.Tick += (tSender, args) => {
                //If on the first run of an iteration, write this text
                if (first) { 
                    Log.Text += $"\n--------------\nITERATION {current}\n--------------\n\n";
                    first = false;
                }

                try {
                    var (oldState, newState, reward, action, newQ) = qrunner.Iterate();
                    UpdateAgent(newState);
                    UpdateLog(newState, reward, action);
                    int idx = Array.IndexOf(new[] { Action.Left, Action.Up, Action.Right, Action.Down }, action);
                    //Update the q-value of the GUI q-chart
                    var relevantQLabel = qLabels.First(x => (string)x.Tag == oldState + " " + idx);
                    relevantQLabel.Content = newQ;
                    relevantQLabel.Background = new SolidColorBrush(GenerateRedYellowGreenGradient(newQ));

                    //Update the q-value arrows on the grid
                    var relevantQCanvas = qArrows.First(x => (string)x.Tag == oldState.ToString());
                    var relevantQArrow = (Grid)relevantQCanvas.Children[idx];
                    ((Polygon)relevantQArrow.Children[0]).Fill = new SolidColorBrush(GenerateRedBlackGreenGradient(newQ));
                    ((Label)relevantQArrow.Children[1]).Content = $"{newQ:0.000}";
                }
                //Game over received, reset state and incrememnt iterations counter "current"
                catch(GameOverException){
                    current++;
                    first = true;

                    UpdateAgent(8);
                    qrunner.Reset();
                }
                //On the last iteration
                if (current > iterations) {
                    //Re-enable everything except pause button
                    RunButton.IsEnabled = true;
                    DiscountFactor.IsEnabled = true;
                    LearningRate.IsEnabled = true;
                    Iterations.IsEnabled = true;
                    LivingPenalty.IsEnabled = true;
                    Delay.IsEnabled = true;
                    ShowHidePolicy.IsEnabled = true;

                    PauseContinue.IsEnabled = false;
                    Log.Text += "\nRun ended.\n";

                    timer.Stop();
                }

            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, delay);
            timer.Start();

        }

        bool paused = false;
        private void PauseContinue_ButtonClick(object sender, RoutedEventArgs e) {
            var btn = sender as Button;
            paused = !paused;

            if(paused) {
                btn.Content = "Continue";
                timer.Stop();
            }
            else {
                btn.Content = "Pause";
                timer.Start();
            }
        }

        private Color GenerateRedYellowGreenGradient(double q) {
            Color result = Colors.Yellow;
            //Clamp q between -1 - 1 (inclusive)
            q = q < -1 ? -1 : q > 1 ? 1 : q;
            if (q < 0) {
                result.ScG += (float)q;
            }
            if (q > 0) {
                result.ScR -= (float)q;
            }

            return result;
        }
        const int CAP = 200;
        private Color GenerateRedBlackGreenGradient(double q) {
            Color result = Colors.Black;
            //Clamp q between -1 - 1 (inclusive)
            q = q < -1 ? -1 : q > 1 ? 1 : q;
            if (q < 0) {
                result.ScR -= (float)q * (CAP / 256f);
            }
            if (q > 0) {
                result.ScG += (float)q * (CAP / 256f);
            }

            return result;
        }

        private void UpdateAgent(int state) {
            Grid.SetColumn(agent, state % 4);
            Grid.SetRow(agent, state / 4);
        }

        private void UpdateLog(int state, double reward, Action action) {
            Log.Text += "Action " + Enum.GetName(typeof(Action), action) + " was taken.\n";
            Log.Text += "Reward: " + reward + "\n";
            Log.Text += "Now in state: " + (state / 4, state % 4) + "\n\n";
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e) {
            Regex regex = new Regex(@"[^0-9.-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        bool shown = false;
        private void ShowHidePolicy_Click(object sender, RoutedEventArgs e) {
            if (!shown) {
                qArrows.ForEach(x => {
                    double[] stateValues = qrunner.qtable[int.Parse((string)x.Tag)];
                    double max = stateValues.Max();
                    int maxIndex = Array.IndexOf(stateValues, max);
                    foreach (var l in x.Children) {
                        var g = l as Grid;
                        g.Visibility = Visibility.Hidden;
                    }
                    agent.Visibility = Visibility.Hidden;

                    //Create image and rotate it
                    Image arrow = new Image
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 5, 0, 0),
                        Height = 80,
                        Width = 80
                    };
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri("Arrow.png", UriKind.Relative);
                    bi.EndInit();
                    arrow.Source = bi;
                    arrow.RenderTransform = new RotateTransform(90 * maxIndex, 40, 40);

                    x.Children.Add(arrow);
                });
                RunButton.IsEnabled = false;
            }
            else {
                qArrows.ForEach(x => {
                    var image = x.Children.Cast<UIElement>().First(l => l is Image) as Image;
                    x.Children.Remove(image);
                    foreach(var l in x.Children) {
                        var g = l as Grid;
                        g.Visibility = Visibility.Visible;
                    }
                    agent.Visibility = Visibility.Visible;
                });
                RunButton.IsEnabled = true;
            }

            Button btn = sender as Button;
            shown = !shown;
            btn.Content = shown ? "Hide Policy" : "Show Policy";
        }
    }
}

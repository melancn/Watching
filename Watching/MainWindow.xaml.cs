using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Watching
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private int run_id = 0;
        private Boolean is_stop = false;

        public MainWindow()
        {
            InitializeComponent();

            this.notifyIcon = new NotifyIcon();
            this.notifyIcon.BalloonTipText = "系统监控中... ...";
            this.notifyIcon.ShowBalloonTip(2000);
            this.notifyIcon.Text = "系统监控中... ...";
            this.notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);
            this.notifyIcon.Visible = true;


            //this.notifyIcon.MouseClick += NotifyIcon_MouseClick;
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler((o, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (this.Visibility == System.Windows.Visibility.Visible) { this.HideWindow(); }
                    else this.ShowWindow();
                }
            });

            addIconMenu();
        }

        private void NotifyIcon_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //addIconMenu();
            }
        }

        private void addIconMenu()
        {
            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem closeItem = new System.Windows.Forms.MenuItem();
            closeItem.Text = "退出";
            closeItem.Click += new EventHandler(delegate { this.Close(); });

            menu.MenuItems.Add(closeItem);
            notifyIcon.ContextMenu = menu;
        }

        private void ShowWindow()
        {
            this.Visibility = System.Windows.Visibility.Visible;
            this.Activate();
        }

        private void HideWindow()
        {
            this.Visibility = System.Windows.Visibility.Hidden;
            notifyIcon.ShowBalloonTip(3000, "监控", "监控最小化到托盘", ToolTipIcon.Info);
        }

        private void CloseWindow()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon = null;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized) { this.HideWindow(); } else this.ShowWindow();

        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            if (run_id > 0)
            {
                return;
            }
            status.Text = "运行中";
            processWatching(url.Text, url_pattern.Text, title.Text, content.Text);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.CloseWindow();
        }

        private async void processWatching(string url, string url_pattern, string title, string content)
        {
            Boolean is_match = this.is_include.IsChecked == true ? true : false;
            HttpClient u = new HttpClient();
            int time_out = int.Parse(interval_time.Text);
            int times = int.Parse(alert_times.Text);
            int count = 0;
            await System.Threading.Tasks.Task.Run(() =>
            {
                run_id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                string html;
                while (true)
                {
                    Console.WriteLine(is_stop);
                    if (is_stop || count >= times)
                    {
                        run_id = 0;
                        is_stop = false;
                        break;
                    }

                    try
                    {
                        html = u.GetStringAsync(url).Result;
                    }
                    catch (HttpRequestException e)
                    {
                        ShowDialog(e.Message);
                        run_id = 0;
                        is_stop = false;
                        break;
                    }
                   
                    Match match = Regex.Match(html, url_pattern);
                    if (is_match)
                    {
                        if (match.Success)
                        {
                            count++;
                            notifyIcon.ShowBalloonTip(time_out, title, content, ToolTipIcon.Info);
                        }
                    }
                    else
                    {
                        if (!match.Success)
                        {
                            count++;
                            notifyIcon.ShowBalloonTip(time_out, title, content, ToolTipIcon.Info);
                        }
                    }
                    Thread.Sleep(time_out);
                }
            });
            status.Text = "已停止";
        }

        private void ShowDialog(string message)
        {
            throw new NotImplementedException();
        }

        private void startHttp()
        {
        }

        private void stop_btn_Click(object sender, RoutedEventArgs e)
        {
            if (run_id > 0)
            {
                status.Text = "正在停止...";
                is_stop = true;
            }
        }
    }
}

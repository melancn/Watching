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
using System.Configuration;
using System.Security;

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

            getConfig();

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
            this.Show();
            WindowState = WindowState.Normal;
            this.Activate();
        }

        private void HideWindow()
        {
            this.Hide();
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

        private void StartStop_Click(object sender, RoutedEventArgs e)
        {
            if (run_id > 0)
            {
                status.Text = "正在停止...";
                is_stop = true;
            }
            else
            {
                saveConfig();
                status.Text = "运行中";
                if ((bool)is_clean_log.IsChecked)
                {
                    logInfo.Text = "";
                }
                processWatching(url.Text, url_pattern.Text, title.Text, content.Text);
                start_stop.Content = "停止";
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.CloseWindow();
        }

        public delegate void dewindow(string a);
        public void addTextToInfo(string a)
        {
            Dispatcher.InvokeAsync(() =>
             {
                 logInfo.Text = logInfo.Text + a + "\r\n";
                 logInfo.ScrollToVerticalOffset(logInfo.ExtentHeight);
             }
            );
        }
        private async void processWatching(string url, string url_pattern, string title, string content)
        {
            Boolean is_match = this.is_include.IsChecked == true ? true : false;
            Boolean is_clean_log = this.is_clean_log.IsChecked == true ? true : false;
            HttpClient u = new HttpClient();
            u.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/88.0");
            int time_out = int.Parse(interval_time.Text);
            int times = int.Parse(alert_times.Text);
            int atime = int.Parse(alert_time.Text);
            int count = 0;
            await System.Threading.Tasks.Task.Run(() =>
            {
                Console.WriteLine("s Thread ID:#{0}", Thread.CurrentThread.ManagedThreadId);
                run_id = System.Threading.Thread.CurrentThread.ManagedThreadId;
                string html;
                string date;
                DateTime now = DateTime.Now;
                while (true)
                {
                    Console.WriteLine(is_stop);
                    if (is_stop || count >= times)
                    {
                        run_id = 0;
                        is_stop = false;
                        System.Windows.MessageBox.Show("已停止");
                        break;
                    }

                    try
                    {
                        html = u.GetStringAsync(url).Result;
                    }
                    catch (HttpRequestException e)
                    {
                        System.Windows.MessageBox.Show(e.Message);
                        run_id = 0;
                        is_stop = false;
                        break;
                    }
                   
                    Match match = Regex.Match(html, url_pattern);
                    if (is_match)
                    {
                        if (match.Success)
                        {
                            if (atime > 0)
                            {
                                DateTime now_t = now.AddMilliseconds(atime);
                                if (now_t > DateTime.Now)
                                {
                                    Thread.Sleep(time_out);
                                    continue;
                                }
                                now = DateTime.Now;
                            }
                            count++;
                            addTextToInfo("提示次数：" + count.ToString());
                            date = DateTime.Now.ToString("u");
                            addTextToInfo(date + " " + title + " " + content);
                            if (notifyIcon != null) notifyIcon.ShowBalloonTip(time_out, title, content, ToolTipIcon.Info);
                        }
                        else
                        {
                            count = 0;
                        }
                    }
                    else
                    {
                        if (!match.Success)
                        {
                            if (atime > 0)
                            {
                                DateTime now_t = now.AddMilliseconds(atime);
                                if (now_t > DateTime.Now)
                                {
                                    Thread.Sleep(time_out);
                                    continue;
                                }
                                now = DateTime.Now;
                            }
                            count++;
                            addTextToInfo("提示次数：" + count.ToString());
                            date = DateTime.Now.ToString("u");
                            addTextToInfo(date + " " + title + " " + content);
                            if (notifyIcon!=null) notifyIcon.ShowBalloonTip(time_out, title, content, ToolTipIcon.Info);
                        }
                        else
                        {
                            count = 0;
                        }
                    }
                    Thread.Sleep(time_out);
                }
            });
            status.Text = "已停止";
            start_stop.Content = "开始";
        }

        private void getConfig()
        {
            try
            {
                title.Text = ConfigurationManager.AppSettings["title"] ?? "DIY卡免还款签账额20元";
                content.Text = ConfigurationManager.AppSettings["content"] ?? "有货";
                alert_times.Text = ConfigurationManager.AppSettings["alert_times"] ?? "10";
                alert_time.Text = ConfigurationManager.AppSettings["alert_time"] ?? "0";
                interval_time.Text = ConfigurationManager.AppSettings["interval_time"] ?? "35000";
                url.Text = ConfigurationManager.AppSettings["url"] ?? "https://shop.cgbchina.com.cn/mall/goods/03140714143403208122?itemCode=03140714143403208122";
                url_pattern.Text = ConfigurationManager.AppSettings["url_pattern"] ?? "stock-zero";
                is_include.IsChecked = ConfigurationManager.AppSettings["is_include"] == "1" ? true : false;
            }
            catch (ConfigurationErrorsException e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
         }
            

        private void saveConfig()
        {
            try
            {
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cfa.AppSettings.Settings.Remove("title");
                cfa.AppSettings.Settings.Add("title", title.Text);
                cfa.AppSettings.Settings.Remove("content");
                cfa.AppSettings.Settings.Add("content", content.Text);
                cfa.AppSettings.Settings.Remove("alert_times");
                cfa.AppSettings.Settings.Add("alert_times", alert_times.Text);
                cfa.AppSettings.Settings.Remove("alert_time");
                cfa.AppSettings.Settings.Add("alert_time", alert_time.Text);
                cfa.AppSettings.Settings.Remove("interval_time");
                cfa.AppSettings.Settings.Add("interval_time", interval_time.Text);
                cfa.AppSettings.Settings.Remove("url");
                cfa.AppSettings.Settings.Add("url", url.Text);
                cfa.AppSettings.Settings.Remove("url_pattern");
                cfa.AppSettings.Settings.Add("url_pattern", url_pattern.Text);
                cfa.AppSettings.Settings.Remove("is_include");
                cfa.AppSettings.Settings.Add("is_include", is_include.IsChecked == true ? "1" : "0");
                cfa.Save();
            }
            catch (ConfigurationErrorsException e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (logInfo.Visibility == System.Windows.Visibility.Visible) {
                logInfo.Visibility = Visibility.Hidden;
            }
            else
            {
                logInfo.Visibility = Visibility.Visible;
            }
        }
    }
}

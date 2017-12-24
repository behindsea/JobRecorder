using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace JobRecorder
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            RichTextBox help = new RichTextBox();
            panel.Children.Add(help);
            help.Margin = new Thickness(0, 20, 0, 0);
            help.AppendText("请在记录仪根目录下新建Config.ini文件，第一行输入“调车”第二行填0-9的数字作为记录仪号码识别");
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            txtPath.Text = "存储目录：" + Properties.Settings.Default["storePath"].ToString();
        }

        private void buttonChange_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog pathDialog = new System.Windows.Forms.FolderBrowserDialog();

            System.Windows.Forms.DialogResult result = pathDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                
                return;
            }
            string storePath = pathDialog.SelectedPath.Trim();
            Properties.Settings.Default["storePath"] = storePath;
            Properties.Settings.Default.Save();

            txtPath.Text = "存储目录：" + Properties.Settings.Default["storePath"].ToString();
        }
    }
}

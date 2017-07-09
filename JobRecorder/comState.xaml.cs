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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JobRecorder
{
    /// <summary>
    /// comState.xaml 的交互逻辑
    /// </summary>
    public partial class ComState : UserControl
    {
        public string State { get; set; }

        public ComState()
        {
            InitializeComponent();
        }

        //进度加一
        public void ProgressInc()
        {
            this.ComProgress.Value += 1;
        }
        //进度完成
        public void ProgressMax()
        {
            ComProgress.Value = ComProgress.Maximum;
        }
        //进度归零
        public void ProgressReSet()
        {
            ComProgress.Value = 0;
        }
        //设置最大进度
        public void SetMaxProgress(int maxProgress)
        {
            ComProgress.Maximum = maxProgress;
        }
        //设置编号
        public void SetNum(string num)
        {
            labNum.Content = string.Format("记录仪编号：{0}", num);
        }
        //状态：传输中
        public void StateCoping()
        {
            State = "Coping";
            labState.Content = "当前状态：传输中…";
        }
        //状态：等待中
        public void StateWating()
        {
            State = "Wating";
            labState.Content = "当前状态：等待中…";
        }
        //状态：传输完成
        public void StateCompete()
        {
            State = "Compete";
            labState.Content = string.Format("当前状态：完毕(本次复制{0}个)", ComProgress.Value.ToString());
        }
        //状态：没有文件
        public void StateNothing()
        {
            State = "Nothing";
            labState.Content = "当前状态：没有找到指定文件";
        }
    }
}

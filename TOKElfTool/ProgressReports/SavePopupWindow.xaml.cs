using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ElfLib;
using ZstdNet;

namespace TOKElfTool
{
    /// <summary>
    /// Interaction logic for SavePopupWindow.xaml
    /// </summary>
    public partial class SavePopupWindow : Window
    {
        private readonly BackgroundWorker worker;
        private readonly ElfBinary<object> binary;
        private readonly string fileSavePath;
        private readonly GameDataType dataType;

        private readonly Compressor compressor = new Compressor();

        public SavePopupWindow(ElfBinary<object> binary, string fileSavePath, GameDataType loadedDataType)
        {
            InitializeComponent();

            this.binary = binary;
            this.fileSavePath = fileSavePath;
            this.dataType = loadedDataType;

            worker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
            };
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        ~SavePopupWindow()
        {
            compressor.Dispose();
        }

        private void SavePopupWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            worker.RunWorkerAsync();
        }


        private void Worker_DoWork(object sender, DoWorkEventArgs ev)
        {
            try
            {
                byte[] serialized = ElfSerializer<object>.SerializeBinary(binary, dataType);
                File.WriteAllBytes(fileSavePath,
                    fileSavePath.EndsWith(".zst") || fileSavePath.EndsWith(".zstd")
                        ? compressor.Wrap(serialized)
                        : serialized);
            }
            catch (Exception e)
            {
                worker.ReportProgress(-1, e);
            }
        }
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case 0:
                    progressBar.Value = ((double)e.UserState) * 100;
                    break;
                case 1:
                    throw (Exception)e.UserState;
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            DialogResult = true;
        }

    }
}

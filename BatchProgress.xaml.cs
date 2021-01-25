using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace ColorMatch3D
{
    /// <summary>
    /// Interaction logic for BatchProgress.xaml
    /// </summary>
    public partial class BatchProgress : Window
    {

        

        public FullyObservableCollection<ProgressItem> ProgressStrings
        {
            get { return progressStrings; }
        }

        public FullyObservableCollection<ProgressItem> progressStrings = new FullyObservableCollection<ProgressItem>();
        public BatchProgress()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public void AddOrUpdateProgressItem(int id, string message="...")
        {

            bool found = false;
            foreach (ProgressItem progressString in progressStrings)
            {
                if(progressString.id == id)
                {

                    Dispatcher.Invoke(() => {

                        progressStrings[progressStrings.IndexOf(progressString)].ProgressText = message;
                    });
                    
                    //progressString.progressText = message;
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Dispatcher.Invoke(()=> {

                    progressStrings.Add(new ProgressItem(message, id));
                });
            }
        }
        public void RemoveProgressItem(int id, string message="...")
        {
            foreach (ProgressItem progressString in progressStrings)
            {
                if(progressString.id == id)
                {
                    progressStrings.Remove(progressString);
                    break;
                }
            }
        }
    }

    public class ProgressItem : INotifyPropertyChanged
    {
        public int id;
        public string ProgressText
        {
            get { return _progressText; }

            set
            {
                _progressText = value;
                RaisePropertyChanged("ProgressText");
            }
        }

        private string _progressText;

        public int Id
        {
            get { return id; }
        }

        public ProgressItem(string progressTextA = "", int currentIndexA = 0)
        {
            ProgressText = progressTextA;
            id = currentIndexA;
        }

        public static implicit operator string(ProgressItem d) => d.ProgressText;
        public static implicit operator ProgressItem(string b) => new ProgressItem(b);
        public override string ToString()
        {
            return ProgressText;
        }


        /// PropertyChanged event handler
        public event PropertyChangedEventHandler PropertyChanged;


        /// Property changed Notification        
        public void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

}

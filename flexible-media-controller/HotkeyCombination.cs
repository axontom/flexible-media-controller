using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace flexible_media_controller
{
    public class HotkeyCombination : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private SortedSet<Key> _keys;
        public SortedSet<Key> Keys
        {
            get { return _keys; }
            set
            {
                _keys = value;
                UpdateText();
            }
        }
        public int Id { get; set; } = -1;
        private string _text;
        public string Text
        {
            get { return _text; }
            private set
            {
                if (value != _text)
                {
                    _text = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public string Label { get; set; }
        public KeyboardCapture.KeyCapturedProc KeyCapturedProc { get; set; }
        private bool _capturing;
        public bool Capturing
        {
            get { return _capturing; }
            set
            {
                _capturing = value;
                UpdateText();
            }
        }
        public void UpdateText()
        {
            if (Keys.Count == 0)
            {
                Text = Capturing ? "waiting for input" : "not assigned";
                return;
            }
            string newText = "";
            bool first = true;
            foreach (var key in _keys)
            {
                if (first)
                    first = false;
                else
                    newText += " + ";
                newText += Enum.GetName(typeof(Key), key);
            }
            Text = newText;
        }
        public void Reset()
        {
            Keys = new SortedSet<Key>();
            Capturing = false;
        }
        public HotkeyCombination(KeyboardCapture.KeyCapturedProc proc, int id,
                                 string label)
        {
            KeyCapturedProc = proc;
            Id = id;
            Label = label;
            Reset();
        }
        public HotkeyCombination()
        {
            Reset();
        }
    }
}

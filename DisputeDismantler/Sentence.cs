using System.Windows.Input;

namespace DisputeDismantler
{
    class Sentence : NotifyPropertyChangedBase
    {
        public const string DD = "DD";
        public const string Guest1 = "Guest-1";
        public const string Guest2 = "Guest-2";

        public Sentence(string user, string text)
        {
            User = user;
            Text = text;

            DoubleTap = new Command(() => User = User == Guest1 ? Guest2 : Guest1, () => User != DD);
            SwipeLeft = new Command(() => User = Guest1, () => User != DD);
            SwipeRight = new Command(() => User = Guest2, () => User != DD);
        }

        public Sentence(string text) : this(DD, text) { }

        public ICommand DoubleTap { get; private set; }
        public ICommand SwipeLeft { get; private set; }
        public ICommand SwipeRight { get; private set; }
        public bool CanSwipeLeft => user == Guest2;
        public bool CanSwipeRight => user == Guest1;
        public SwipeDirection ExpectingSwipeDirection { get => user == Guest1 ? SwipeDirection.Right : user == Guest2 ? SwipeDirection.Left : SwipeDirection.Down; }

        string user;
        public string User 
        {
            get => user;
            set => SetProperty(ref user, value, nameof(User), nameof(Margin), nameof(BkgColor), nameof(CanSwipeLeft), nameof(CanSwipeRight),
                nameof(DoubleTap), nameof(ExpectingSwipeDirection));
        }

        string text;
        public string Text { get => text; set => SetProperty(ref text, value); }

        public Thickness Margin { get => user == Guest1 ? new Thickness(0, 0, 100, 0) : user == Guest2 ? new Thickness(100, 0, 0, 0) : Thickness.Zero; }

        public Color BkgColor { get => user == Guest1 ? Colors.AliceBlue : user == Guest2 ? Colors.AntiqueWhite : Colors.LightCyan; }
    };
}

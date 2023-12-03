using System.ComponentModel;

namespace DisputeDismantler
{
    public partial class MainPage : ContentPage
    {
        Transcriber t;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = t = new Transcriber();
            t.ConversationChanged += ConversationChanged;
        }

        bool isPendingScroll;
        async void ConversationChanged(object? sender, EventArgs e)
        {
            if (!isPendingScroll)
            {
                isPendingScroll = true;
                await Task.Delay(250);
                isPendingScroll = false;
                if (MainThread.IsMainThread)
                    AutoScroll();
                else
                    MainThread.BeginInvokeOnMainThread(AutoScroll);
            }
        }

        void AutoScroll()
        {
            if (t.Conversation.Count > 0) lstConversation.ScrollTo(t.Conversation.Count - 1); // MAUI bug? Does not work sometimes...
        }
    }
}

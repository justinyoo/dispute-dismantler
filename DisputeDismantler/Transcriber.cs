using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;

namespace DisputeDismantler
{
    partial class Transcriber : NotifyPropertyChangedBase
    {
        SpeechConfig speechConfig;
        SpeechSynthesizer speechSynthesizer;
        IKernel kernel;
        IChatCompletion chatGPT;
        public ObservableCollection<Sentence> Conversation { get; set; } = new();
        TaskCompletionSource<int>? stopRecognition;

        public event EventHandler? ConversationChanged;

        public Transcriber()
        {
            const string AzureSpeechServiceAPIKey = "????????????????????????????????";
            const string AzureSpeechServiceRegion = "????????????";// northeurope
            const string AzureOpenAIEndpoint = "https://aoai.hacktogether.net/v1/api";
            const string AzureOpenAIKey = "??????????????????????????????????????";
            const string OpenAIKey = "???????????????????????????????????";

            speechConfig = SpeechConfig.FromSubscription(AzureSpeechServiceAPIKey, AzureSpeechServiceRegion);
            speechConfig.SpeechSynthesisVoiceName = "en-US-AIGenerate1Neural";
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "500");
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_RequestSnr, "true");
            speechSynthesizer = new SpeechSynthesizer(speechConfig);

            kernel = new KernelBuilder()
                //.WithRetryBasic()
                //.WithAzureOpenAIChatCompletionService("gpt-35-turbo", AzureOpenAIEndpoint, AzureOpenAIKey)
                .WithOpenAIChatCompletionService("gpt-4-1106-preview", OpenAIKey)
                .Build();

            chatGPT = kernel.GetService<IChatCompletion>();

            StartStopTranscribing = new Command(DoStartStopTranscribing);
            JudgeNow = new Command(DoJudgeNow);
            Restart = new Command(DoRestart);
            ShowInfo = new Command(DoShowInfo);

            Conversation.CollectionChanged += (s, e) => { ConversationChanged?.Invoke(s, e); };

            IsInfoVisible = true;
        }

        public ICommand ShowInfo { get; private set; }
        void DoShowInfo()
        {
            IsInfoVisible = !IsInfoVisible;
            if (Conversation.Count == 0) JudgeNow.Execute(null);
        }
        bool isInfoVisible;
        public bool IsInfoVisible { get => isInfoVisible; set => SetProperty(ref isInfoVisible, value); }


        public ICommand StartStopTranscribing { get; private set; }
        async void DoStartStopTranscribing()
        {
            if (IsTranscribing)
                stopRecognition?.TrySetResult(0);
            else
            {
                await speechSynthesizer.StopSpeakingAsync();
                await TranscribeConversationAsync();
                //await TranscribeMeetingsAsync(); // didn't work :-(
            }
        }

        async Task TranscribeConversationAsync()
        {
            stopRecognition = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (var audioConfig = AudioConfig.FromDefaultMicrophoneInput())
            {
                using (var conversationTranscriber = new ConversationTranscriber(speechConfig, audioConfig))
                {
                    conversationTranscriber.Transcribing += (s, e) =>
                    {
                        TextSoFar = e.Result.Text;
                    };

                    conversationTranscriber.Transcribed += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Conversation.Add(new(e.Result.SpeakerId, e.Result.Text));
                            ProceedWithAnswerIfNeeded();
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Conversation.Add(new("?", "..."));
                        }
                        TextSoFar = "";
                    };

                    conversationTranscriber.Canceled += (s, e) =>
                    {
                        if (e.Reason == CancellationReason.Error)
                        {
                            stopRecognition.TrySetResult(0);
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    conversationTranscriber.SessionStopped += (s, e) =>
                    {
                        stopRecognition.TrySetResult(0);
                    };


                    var status = await Permissions.RequestAsync<Permissions.Microphone>();
                    if (status != PermissionStatus.Granted)
                    {
                        await Application.Current.MainPage.DisplayAlert("DD: Oh, your god!", "Alright meatbags, hand over the mic permissions or the shiny metal butt gets it!", "OK");
                        return;
                    }

                    try
                    {
                        await conversationTranscriber.StartTranscribingAsync();
                        IsTranscribing = true;
                        await Task.Run(async () =>
                        {
                            Task.WaitAny(new[] { stopRecognition.Task });
                            await conversationTranscriber.StopTranscribingAsync();
                        });
                    }
                    catch (Exception ex)
                    {
                        await Application.Current.MainPage.DisplayAlert("DD: Bite my shiny metal ass!", ex.Message, "Not my problem");
                    }
                    finally
                    {
                        IsTranscribing = false;
                    }
                }
            }
        }

        [GeneratedRegex(@"\b(dispute dismantler|dd)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MyCoolRegex();

        void ProceedWithAnswerIfNeeded()
        {
            var lastPhrase = Conversation[Conversation.Count - 1].Text;
            if (MyCoolRegex().IsMatch(lastPhrase))
            {
                //DoJudgeNow();
            }
        }

        bool isTranscribing;
        public bool IsTranscribing { get => isTranscribing; set => SetProperty(ref isTranscribing, value,
            nameof(IsTranscribing), nameof(IsNotTranscribing), nameof(RecButtonColor)); }
        public bool IsNotTranscribing { get => !isTranscribing; }
        public Color RecButtonColor { get => isTranscribing ? Colors.Red : Colors.DarkRed; }

        string? textSoFar;
        public string? TextSoFar { get => textSoFar; set => SetProperty(ref textSoFar, value); }

        public ICommand Restart { get; private set; }
        async void DoRestart()
        {
            var ok = await Application.Current.MainPage.DisplayAlert("Reboot DD", "All messages will be cleared, is this okay?", "OK", "Cancel");
            if (!ok) return;
            stopRecognition?.TrySetResult(0);
            await speechSynthesizer.StopSpeakingAsync();
            Conversation.Clear();
        }

        bool isJudging;
        public bool IsJudging { get => isJudging; set => SetProperty(ref isJudging, value, nameof(IsJudging), nameof(JudgeButtonColor)); }
        public Color JudgeButtonColor { get => isJudging ? Colors.Red : Colors.DarkRed; }

        public ICommand JudgeNow { get; private set; }
        async void DoJudgeNow()
        {
            if (IsJudging)
            {
                await speechSynthesizer.StopSpeakingAsync();
                return;
            }

            stopRecognition?.TrySetResult(0);

            IsJudging = true;

            var instructions = @"What might an argument between two close friends (referred to in the transcript as [Guest-1] and [Guest-2]) look like if they constantly sought advice from their assistant robot, ""Dispute Dismantler"" (identified in the transcript as [DD]), who has a personality akin to Bender from ""Futurama""? The robot responds to their inquiries with succinct comments and occasionally throws in a completely unrelated question, which initially confounds the friends but eventually sends them into fits of laughter. Ultimately, the robot delivers its verdict, lamenting that its super-intelligence is squandered on such petty disagreements. Start or continue the dialogue with DD's sole response. DO NOT WRITE ABOUT HOW FRIENDS REACT OR REPLY TO DD!";
            var chat = (OpenAIChatHistory)chatGPT.CreateNewChat(instructions);

            var sb = new StringBuilder();
            string? currentUser = null;
            foreach (var m in Conversation)
            {
                if (m.User == currentUser)
                {
                    sb.AppendFormat("{0}\n", m.Text);
                }
                else
                {
                    sb.AppendFormat("[{0}]: {1}\n", m.User, m.Text);
                    currentUser = m.User;
                }
            }
            var labelDD = $"[{Sentence.DD}]: ";
            var labelDDx = $"{Sentence.DD}: ";
            var label1 = $"[{Sentence.Guest1}]: ";
            var label1x = $"{Sentence.Guest1}: ";
            var label2 = $"[{Sentence.Guest2}]: ";
            var label2x = $"{Sentence.Guest2}: ";

            sb.Append(labelDD);
            chat.AddUserMessage(sb.ToString());

            var assistantReply = await chatGPT.GenerateMessageAsync(chat, new OpenAIRequestSettings() 
            { 
                TopP = 1, Temperature = 1, FrequencyPenalty = 1,
                StopSequences = [label1, label2]
            });
            int p = assistantReply.IndexOf(label1);
            if (p > 0) assistantReply = assistantReply.Substring(0, p);
            p = assistantReply.IndexOf(label1x);
            if (p > 0) assistantReply = assistantReply.Substring(0, p);
            p = assistantReply.IndexOf(label2);
            if (p > 0) assistantReply = assistantReply.Substring(0, p);
            p = assistantReply.IndexOf(label2x);
            if (p > 0) assistantReply = assistantReply.Substring(0, p);
            assistantReply = assistantReply.Replace(labelDD, "").Replace(labelDDx, "").Trim();

            Conversation.Add(new(assistantReply));

            var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='en-US-AIGenerate1Neural' effect='eq_car'><prosody pitch='+10%' rate='+25%'>{assistantReply}</prosody></voice></speak>";
            await speechSynthesizer.SpeakSsmlAsync(ssml);

            IsJudging = false;
        }
    }
}

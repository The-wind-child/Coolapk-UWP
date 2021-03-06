﻿using CoolapkUWP.Helpers;
using CoolapkUWP.Models.Controls.MakeFeedControlModel;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

namespace CoolapkUWP.Controls
{
    public enum MakeFeedMode
    {
        Feed,
        Reply,
        ReplyReply
    }

    public sealed partial class MakeFeedControl : UserControl
    {
        public event EventHandler MakedFeedSuccessful;

        public MakeFeedMode MakeFeedMode { get; set; }

        public string FeedId { get; set; }

        public MakeFeedControl()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Document.GetText(TextGetOptions.UseObjectText, out string contentText);
            contentText = contentText.Replace("\r", "\r\n");
            if (string.IsNullOrWhiteSpace(contentText)) return;
            using (var content = new HttpMultipartFormDataContent(GetBoundary()))
            {
                switch (MakeFeedMode)
                {
                    case MakeFeedMode.Feed:
                        using (var a = new HttpStringContent(contentText))
                        using (var b = new HttpStringContent("feed"))
                        using (var c = new HttpStringContent("0"))
                        {
                            content.Add(a, "message");
                            content.Add(b, "type");
                            content.Add(c, "is_html_article");
                            if (await DataHelper.PostDataAsync(DataUriType.CreateFeed, content) != null)
                            {
                                SendSuccessful();
                            }
                        }
                        break;

                    case MakeFeedMode.Reply:
                    case MakeFeedMode.ReplyReply:
                        var type = MakeFeedMode == MakeFeedMode.Reply ? DataUriType.CreateFeedReply : DataUriType.CreateReplyReply;
                        using (var d = new HttpStringContent(contentText))
                        {
                            content.Add(d, "message");
                            if (await DataHelper.PostDataAsync(type, content, FeedId) != null)
                            {
                                SendSuccessful();
                            }
                        }
                        break;
                }
            }
        }

        private void SendSuccessful()
        {
            UIHelper.ShowMessage(Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("MakeFeedControl").GetString("sendSuccessed"));
            InputBox.Document.SetText(TextSetOptions.None, string.Empty);
            MakedFeedSuccessful?.Invoke(this, new EventArgs());
        }

        private string GetBoundary()
        {
            byte[] vs = new byte[16];
            new Random().NextBytes(vs);
            var builder = new System.Text.StringBuilder();
            foreach (var item in vs)
                builder.Append(Convert.ToString(item, 16));
            builder.Insert(8, "-");
            builder.Insert(13, "-");
            builder.Insert(18, "-");
            builder.Insert(23, "-");
            return builder.ToString();
        }

        private async void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            await InsertEmoji(e.ClickedItem as EmojiData);
        }

        private async Task InsertEmoji(EmojiData data)
        {
            InputBox.Document.Selection.InsertImage(20,
                                                    20,
                                                    0,
                                                    VerticalCharacterAlignment.Baseline,
                                                    data.Name,
                                                    await (await StorageFile.GetFileFromApplicationUriAsync(data.Uri)).OpenReadAsync());
            InputBox.Document.Selection.MoveRight(TextRangeUnit.Character, 1, false);
        }

        private void InputBox_ContextMenuOpening(object sender, ContextMenuEventArgs e) => e.Handled = true;

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (string.IsNullOrEmpty(sender.Text) || args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) { return; }

            sender.ItemsSource = (from s in EmojiTypeHelper.TypeHeaders
                                  from emoji in s.Emojis
                                  where emoji.Name.Contains(sender.Text, StringComparison.OrdinalIgnoreCase)
                                  select emoji);
        }

        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = (args.SelectedItem as EmojiData).Name;
        }

        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null) { return; }
            await InsertEmoji(args.ChosenSuggestion as EmojiData);
        }
    }
}

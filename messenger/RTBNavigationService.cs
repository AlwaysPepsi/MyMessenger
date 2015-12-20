using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace RichTextBox_Hyperlink
{
    public static class RTBNavigationService
    {
        private static readonly Regex regexUrl = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");
        private static readonly Regex regexSmilies = new Regex(@"(:\)(?!\)))");

        public static readonly DependencyProperty ContentProperty = DependencyProperty.RegisterAttached(
            "Content",
            typeof(string),
            typeof(RTBNavigationService),
            new PropertyMetadata(null, OnContentChanged)
        );

        public static string GetContent(DependencyObject d)
        { return d.GetValue(ContentProperty) as string; }

        public static void SetContent(DependencyObject d, string value)
        { d.SetValue(ContentProperty, value); }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox richTextBox = d as RichTextBox;
            if (richTextBox == null)
                return;

            string content = (string)e.NewValue;
            if (string.IsNullOrEmpty(content))
                return;

            richTextBox.Document.Blocks.Clear();

            int lastPos = 0;
            Paragraph block = new Paragraph();
            foreach (Match match in regexSmilies.Matches(content))
            {
                if (match.Index != lastPos)
                    block.Inlines.Add(content.Substring(lastPos, match.Index - lastPos));

                BitmapImage bitmapSmiley = new BitmapImage(new Uri("giggle.gif", UriKind.Relative));
                Image smiley = new Image();
                smiley.Source = bitmapSmiley;
                smiley.Width = bitmapSmiley.Width;
                smiley.Height = bitmapSmiley.Height;
                block.Inlines.Add(smiley);

                lastPos = match.Index + match.Length;
            }
            if (lastPos < content.Length)
                block.Inlines.Add(content.Substring(lastPos));
            richTextBox.Document.Blocks.Add(block);

            List<Hyperlink> results = new List<Hyperlink>();
            //foreach (Match match in regexUrl.Matches(content.Replace(Environment.NewLine,"")))
            foreach (Match match in regexUrl.Matches(content))
            {
                TextPointer p1 = richTextBox.ToTextPointer(match.Index);
                TextPointer p2 = richTextBox.ToTextPointer(match.Index + match.Length);
                if (p1 == null || p2 == null)
                {
                    //Donothing
                }
                else
                {
                    (new Hyperlink(p1, p2)).Click += OnUrlClick;
                }
            }
        }

        private static void OnUrlClick(object sender, RoutedEventArgs e)
        {
            Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }

        public static TextPointer ToTextPointer(this RichTextBox rtb, int index)
        {
            int count = 0;
            TextPointer position = rtb.Document.ContentStart.GetNextContextPosition(LogicalDirection.Forward).GetNextContextPosition(LogicalDirection.Forward);
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);
                    int length = textRun.Length;
                    if (count + length > index)
                    {
                        return position.GetPositionAtOffset(index - count);
                    }
                    count += length;
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }
            return null;
        }
    }
}
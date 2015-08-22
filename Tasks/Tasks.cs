using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Tasks
{
    public sealed class ToastBackground : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            //Check if a background toast is already scheduled
            if (ToastNotificationManager.CreateToastNotifier().GetScheduledToastNotifications().Select(x => x.Id = "Background").Count() > 0)
            {
                return;
            }

            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText02;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode("League of Legends"));
            toastTextElements[1].AppendChild(toastXml.CreateTextNode("New Champion data has arrived. Keep up with the Meta now!"));

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-appx:///Assets/yourturn.mp3");
            toastNode.AppendChild(audio);

            ToastNotification toast = new ToastNotification(toastXml);
            DateTime dueTime = DateTime.Now.AddHours(72);
            ScheduledToastNotification scheduledToast = new ScheduledToastNotification(toastXml, dueTime);
            scheduledToast.Id = "Background";
            ToastNotificationManager.CreateToastNotifier().AddToSchedule(scheduledToast);
        }
    }
}

using Il2CppYgomSystem.UI;

namespace BlindDuel
{
    public class NotificationsHandler : IMenuHandler
    {
        public bool CanHandle(string viewControllerName) =>
            viewControllerName is "NotificationMenu" or "Notifications";

        public void OnScreenEntered(string viewControllerName) { }

        public string OnButtonFocused(SelectionButton button)
        {
            if (button.transform.Find("BaseCategory") != null)
                return HomeHandler.ReadNotificationText(button);

            return null;
        }
    }
}

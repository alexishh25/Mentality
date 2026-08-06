using UnityEngine.UIElements;

namespace Mentality.UI.Views
{
    [UxmlElement]
    public partial class MenuButton : Button
    {
        public MenuButton()
        {
            AddToClassList("menu-button");
        }
    }
}

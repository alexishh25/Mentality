using UnityEngine.UIElements;

namespace Mentality.Presentation.Views
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

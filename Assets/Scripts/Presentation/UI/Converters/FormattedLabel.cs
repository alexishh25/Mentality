using UnityEngine.UIElements;

namespace Mentality.Presentation.Views
{
    [UxmlElement]
    public partial class FormattedLabel : Label
    {
        [UxmlAttribute("format")]
        public string FormatString { get; set; } = "{0}";
        
        private string _rawValue = "";

        [UxmlAttribute("raw-value")]
        public string RawValue
        {
            get => _rawValue;
            set
            {
                _rawValue = value;
                text = string.Format(FormatString, _rawValue);
            }
        }
    }

}
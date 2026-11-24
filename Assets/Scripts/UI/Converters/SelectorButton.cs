using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.Presentation.Views
{
    public class FieldListConverter : UxmlAttributeConverter<List<Fields>>
    {
        const char k_ItemSeparator = ',';
        const char k_KeyValueSeparator = ':';

        public override string ToString(List<Fields> value)
        {
            if (value == null) return string.Empty;
            List<string> parts = new List<string>();
            foreach (var item in value)
            {
                if (item != null)
                {
                    parts.Add($"{item.Content}{k_KeyValueSeparator}{item.onSelected}");
                }
            }
            return string.Join(k_ItemSeparator.ToString(), parts);
        }

        public override List<Fields> FromString(string value)
        {
            var list = new List<Fields>();
            if (string.IsNullOrEmpty(value)) return list;

            var items = value.Split(k_ItemSeparator);
            foreach (var item in items)
            {
                var trimmed = item.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var split = trimmed.Split(k_KeyValueSeparator);
                if (split.Length == 2)
                {
                    list.Add(new Fields
                    {
                        Content = split[0].Trim(),
                        onSelected = split[1].Trim()
                    });
                }
                else
                {
                    list.Add(new Fields
                    {
                        Content = trimmed,
                        onSelected = string.Empty
                    });
                }
            }
            return list;
        }
    }

    [Serializable]
    public class Fields
    {
        public string Content;
        public string onSelected;
    }

    [UxmlElement]
    public partial class SelectorButton : VisualElement
    {

        private string _content = null;

        [UxmlAttribute("content")]
        public string Content
        {
            get => _content ?? string.Empty;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    if (_description != null)
                    {
                        _description.text = _content;
                    }
                }
            }
        }

        private List<Fields> _selections = null;

        [UxmlAttribute("selections")]
        public List<Fields> Selections
        {
            get => _selections ?? (_selections = new List<Fields>());
            set
            {
                _selections = value ?? new List<Fields>();
                _selectedIndex = 0; // Reiniciamos el índice al cambiar las opciones
                UpdateDisplay();
            }
        }

        private int _selectedIndex = 0;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value)
                {
                    _selectedIndex = value;
                    UpdateDisplay();
                }
            }
        }


        private Button _leftButton;
        private Button _rightButton;
        private Label _label;
        private Label _description;

        public SelectorButton()
        {
            // Añadimos la clase base al control
            AddToClassList("selector-button");

            // 1. Etiqueta de descripción (ej: "Resolución" o "Audio")
            _description = new Label { text = Content };
            _description.name = "Description";
            _description.AddToClassList("selector-button__description");
            Add(_description);

            // Contenedor para agrupar los controles interactivos [ ◀ ] [ Valor ] [ ▶ ]
            var controlsContainer = new VisualElement();
            controlsContainer.AddToClassList("selector-button__controls-container");
            Add(controlsContainer);

            // 2. Botón Izquierdo (Retroceder)
            _leftButton = new Button();
            _leftButton.name = "LeftB";
            _leftButton.AddToClassList("selector-button__arrow");
            _leftButton.AddToClassList("selector-button__arrow--left");
            _leftButton.clicked += OnLeftClicked;
            controlsContainer.Add(_leftButton);

            // 3. Etiqueta del valor seleccionado
            _label = new Label();
            _label.name = "Content";
            _label.AddToClassList("selector-button__label");
            controlsContainer.Add(_label);

            // 4. Botón Derecho (Avanzar)
            _rightButton = new Button();
            _rightButton.name = "RightB";
            _rightButton.AddToClassList("selector-button__arrow");
            _rightButton.AddToClassList("selector-button__arrow--right");
            _rightButton.clicked += OnRightClicked;
            controlsContainer.Add(_rightButton);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_label == null) return;

            if (_selections == null || _selections.Count == 0)
            {
                _label.text = string.Empty;
                return;
            }

            // Mantenemos el índice dentro del rango de índices válidos
            if (_selectedIndex < 0) _selectedIndex = 0;
            if (_selectedIndex >= _selections.Count) _selectedIndex = _selections.Count - 1;

            _label.text = _selections[_selectedIndex].Content;
        }

        private void OnLeftClicked()
        {
            if (_selections != null && _selections.Count > 0)
            {
                SelectedIndex = (_selectedIndex - 1 + _selections.Count) % _selections.Count;
            }
        }

        private void OnRightClicked()
        {
            if (_selections != null && _selections.Count > 0)
            {
                SelectedIndex = (_selectedIndex + 1) % _selections.Count;
            }
        }

        private void ApplyLogic()
        {
            if (_selections == null || _selectedIndex < 0 || _selectedIndex >= _selections.Count) return;

            string action = _selections[_selectedIndex].onSelected;
            if (!string.IsNullOrEmpty(action))
            {
                Debug.Log($"[SelectorButton] Acción ejecutada: {action}");
            }
        }
    }
}
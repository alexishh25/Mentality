using UnityEngine;
using UnityEngine.UI;

namespace Mentality.Presentation.Views
{
    /// <summary>
    /// Single Responsibility Principle (SRP).
    /// Responsable únicamente de la representación visual de la barra de vida en la interfaz de usuario.
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Text healthText; // Opcional

        public void Setup(int maxHealth)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = maxHealth;
            }
        }

        public void UpdateHealthUI(int currentHealth, int maxHealth)
        {
            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {currentHealth} / {maxHealth}";
            }
            
            Debug.Log($"[HealthBarView] UI Actualizada -> HP: {currentHealth} / {maxHealth}");
        }
    }
}

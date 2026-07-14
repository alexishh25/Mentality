using UnityEngine;
using Mentality.Domain.Models;
using Mentality.Application.Interfaces;
using Mentality.Infrastructure.Persistence;
using Mentality.Presentation.Views;
using Mentality.Presentation.Presenters;

namespace Mentality.Presentation.Controllers
{
    /// <summary>
    /// Single Responsibility Principle (SRP) & Dependency Inversion Principle (DIP).
    /// El PlayerController maneja la interacción directa con el jugador, el flujo de entrada
    /// de Unity y la invocación de las acciones.
    /// Utiliza inversión de dependencias para guardar/cargar la vida a través de la interfaz ISaveService.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Configuración de Salud")]
        [SerializeField] private int maxHealth = 100;

        [Header("Componentes Visuales")]
        [SerializeField] private HealthBarView healthBarView;

        private Health _health;
        private HealthPresenter _healthPresenter;

        // Abstracción de servicio (DIP)
        private ISaveService _saveService;

        private void Awake()
        {
            _health = new Health(maxHealth, maxHealth);

            // Inyectamos la dependencia concreta.
            // Nota: En un proyecto más avanzado con frameworks como Zenject/VContainer, esto se inyectaría
            // automáticamente en el constructor o a través de atributos.
            _saveService = new JsonSaveService(); 

            // Vinculamos el modelo con la vista usando el Presentador (MVP)
            if (healthBarView != null)
            {
                _healthPresenter = new HealthPresenter(_health, healthBarView);
            }

            _health.OnDeath += HandlePlayerDeath;
        }

        private void Update()
        {
            // Ejemplo de entrada para probar el sistema de salud (SRP: el controller capta el input)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                TakeDamageTest(10);
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                HealTest(10);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SavePlayerState();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadPlayerState();
            }
        }

        public void TakeDamageTest(int amount)
        {
            _health.TakeDamage(amount);
        }

        public void HealTest(int amount)
        {
            _health.Heal(amount);
        }

        private void HandlePlayerDeath()
        {
            Debug.Log("[PlayerController] ¡El jugador ha muerto!");
        }

        private void SavePlayerState()
        {
            _saveService.Save("player_health", _health.CurrentHealth);
            Debug.Log($"[PlayerController] Estado de salud guardado: {_health.CurrentHealth}");
        }

        private void LoadPlayerState()
        {
            int savedHealth = _saveService.Load("player_health", maxHealth);
            _health.RestoreState(savedHealth);
            Debug.Log($"[PlayerController] Estado de salud cargado: {savedHealth}");
        }

        private void OnDestroy()
        {
            _health.OnDeath -= HandlePlayerDeath;
            _healthPresenter?.Dispose();
        }
    }
}

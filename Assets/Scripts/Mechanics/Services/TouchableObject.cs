using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Gameplay
{
    public class TouchableObject : MonoBehaviour, ITouchable
    {
        [SerializeField] private string objectId = "box_01";
        [SerializeField] private string prefabName = "MagicCubePrefab";
        [SerializeField] private GameObject prefabToSpawn;

        private int touchCount = 0;
        private IApiClient apiClient;

        [Inject]
        public void Construct(IApiClient apiClient)
        {
            this.apiClient = apiClient;
        }

        // Método invocado por el Presenter a través de la interfaz ITouchable
        public void Touch(Vector3 playerPosition)
        {
            touchCount++;

            if (touchCount > 3)
            {
                EnviarEventoToqueAsync(playerPosition).Forget();
            }
        }

        private async UniTaskVoid EnviarEventoToqueAsync(Vector3 playerPosition)
        {
            try { 
            var requestData = new EventoToqueObjetoRequest
            {
                player_id = "Player_01",
                object_id = objectId,
                prefab_name = prefabName,
                touch_count = touchCount,
                player_position = new Vector3Data(playerPosition)
            };

                var response = await apiClient.Post<EventoToqueObjetoRequest, ComandoDuplicadoResponse>(
                            "/gameplay/object-touch",
                            requestData
                        );

                Debug.Log($"Respuesta recibida: {response?.action}");

                if (response != null && response.action == "SPAWN_TEMPORARY_OBJECTS")
                {
                    ProcesarDuplicado(response);
                    touchCount = 0;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Excepción capturada en la llamada API: {ex.Message}");
            }
        }

        private void ProcesarDuplicado(ComandoDuplicadoResponse respuesta)
        {
            List<GameObject> objetosCreados = new List<GameObject>();

            foreach (var pos in respuesta.positions)
            {
                GameObject nuevoObjeto = Instantiate(prefabToSpawn, pos.ToVector3(), Quaternion.identity);
                objetosCreados.Add(nuevoObjeto);
            }

            DestruirDespuesDeTiempoAsync(objetosCreados, respuesta.duration_seconds).Forget();
        }

        private async UniTaskVoid DestruirDespuesDeTiempoAsync(List<GameObject> objetos, float segundos)
        {
            await UniTask.Delay((int)(segundos * 1000));

            foreach (var obj in objetos)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }
    }
}
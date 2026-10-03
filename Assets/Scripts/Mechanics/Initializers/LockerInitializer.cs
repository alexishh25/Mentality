using UnityEngine;
using VContainer;

namespace Gameplay
{
    public class LockerInitializer : MonoBehaviour
    {
        private LockerFactory lockerFactory;

        [Inject]
        public void Construct(LockerFactory lockerFactory)
        {
            this.lockerFactory = lockerFactory;
            InitializeLockers();
        }

        private void InitializeLockers()
        {
            // Encuentra todos los casilleros en la escena
            var lockers = FindObjectsByType<LockerView>(FindObjectsSortMode.None);

            foreach (var locker in lockers)
            {
                lockerFactory.CreatePresenter(locker, locker.ConsumablePrefab);
            }
        }
    }
}
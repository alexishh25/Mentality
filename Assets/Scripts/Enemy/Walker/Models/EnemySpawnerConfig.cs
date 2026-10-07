using System;
using UnityEngine;

namespace Gameplay.Enemy.Walker.Models
{
    [Serializable]
    public class EnemySpawnerConfig
    {
        public GameObject EnemyPrefab;
        public Transform[] SpawnPoints;
        public float SpawnInterval = 5f;
        public float MinSpawnDistance = 10f;
    }
}
using System.Collections.Generic;
using Assets.Scripts.Core.Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.Generator
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Floor Settings")]
        [SerializeField] private GameObject[] floorPrefabs;
        [SerializeField] private GameObject _firstFloor;
        [SerializeField] private float floorHeight = 6f;

        private ObjectPool<GameObject> pool;
        private Dictionary<int, GameObject> activeFloors = new Dictionary<int, GameObject>();
        private int highestFloor = 1;

        void Awake()
        {
            pool = new ObjectPool<GameObject>(
                createFunc: () => Instantiate(GetRandomPrefab(), transform),
                actionOnGet: (obj) => obj.SetActive(true),
                actionOnRelease: (obj) => obj.SetActive(false),
                actionOnDestroy: (obj) => Destroy(obj),
                defaultCapacity: 10,
                maxSize: 50
            );
        }

        void Start()
        {
            // lantai 1 statis
            activeFloors[1] = _firstFloor;
            _firstFloor.transform.position = new Vector3(0, 0, 0); // pastikan di posisi dasar
            highestFloor = 1;

            // spawn awal lantai 2 & 3
            SpawnFloor(2);
            SpawnFloor(3);

            // subscribe ke event scoreManager
            ScoreManager.Instance.OnNextStage += HandleScoreChanged;
            ScoreManager.Instance.OnRecycleStage += HandleDespwan;
        }

        private void OnDestroy()
        {
            // jangan lupa unsubscribe biar aman
            ScoreManager.Instance.OnNextStage -= HandleScoreChanged;
            ScoreManager.Instance.OnRecycleStage -= HandleDespwan;
        }

        private void HandleScoreChanged(int currentFloor)
        {
            int nextFloor = highestFloor + 1;
            SpawnFloor(nextFloor);

            // tentukan lantai yang harus direcycle (misalnya 2 lantai di bawah player)

        }

        private void HandleDespwan(int currentFloor)
        {
            if (currentFloor >= 3)
            {
                int floorToRecycle = currentFloor - 2;
                if (activeFloors.ContainsKey(floorToRecycle))
                {
                    pool.Release(activeFloors[floorToRecycle]);
                    activeFloors.Remove(floorToRecycle);
                }
            }
        }

        private void SpawnFloor(int floorNumber)
        {
            GameObject floor = pool.Get();
            floor.transform.position = new Vector3(0, (floorNumber - 1) * floorHeight, 0);

            if (floor.TryGetComponent<Floor>(out var floorComp))
            {
                bool isEven = floorNumber % 2 == 0;
                floorComp.SetActiveLevel(isEven);
            }

            activeFloors[floorNumber] = floor;
            highestFloor = Mathf.Max(highestFloor, floorNumber);
        }

        private GameObject GetRandomPrefab()
        {
            return floorPrefabs[Random.Range(0, floorPrefabs.Length)];
        }
    }
}

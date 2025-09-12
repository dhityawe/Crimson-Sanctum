using System.Collections.Generic;
using Assets.Scripts.Core.Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace Assets.Scripts.Generator
{
    public class LevelGenerator : MonoBehaviour
    {
        [Header("Floor Settings")]
        [SerializeField] private GameObject[] floorPrefabs;   // lantai random
        [SerializeField] private GameObject _firstFloor;
        [SerializeField] private float floorHeight = 6f;
        [SerializeField] private ScoreManager scoreManager;

        private ObjectPool<GameObject> pool;
        private Dictionary<int, GameObject> activeFloors = new Dictionary<int, GameObject>();
        private int highestFloor = 1; // mulai dari lantai 1 (sudah ada di hierarchy)

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
            // lantai 1 sudah statis → langsung tambahin record
            // supaya tidak di-spawn ulang
            activeFloors[1] = _firstFloor; 

            // spawn lantai 2 & 3
            SpawnFloor(2);
            SpawnFloor(3);
        }

        void Update()
        {
            int currentFloor = scoreManager.Score;

            if (currentFloor > 1 && currentFloor % 2 == 0)
            {
                int nextFloor = highestFloor + 1;
                SpawnFloor(nextFloor);

                int floorToRecycle = currentFloor - 2;
                if (activeFloors.ContainsKey(floorToRecycle))
                {
                    pool.Release(activeFloors[floorToRecycle]);
                    activeFloors.Remove(floorToRecycle);
                }
            }
        }

        void SpawnFloor(int floorNumber)
        {
            GameObject floor = pool.Get();
            floor.transform.position = new Vector3(0, (floorNumber - 1) * floorHeight, 0);

            Floor floorComp = floor.GetComponent<Floor>();
            if (floor != null)
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
